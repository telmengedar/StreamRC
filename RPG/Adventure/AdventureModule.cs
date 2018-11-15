using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using NightlyCode.Modules;
using StreamRC.Core.Timer;
using StreamRC.RPG.Adventure.Commands;
using StreamRC.RPG.Adventure.Exploration;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters;
using StreamRC.RPG.Adventure.SpiritRealm;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Adventure {

    [Module(Key="adventure")]
    public class AdventureModule : ITimerService {
        readonly StreamModule stream;
        readonly PlayerModule players;
        readonly object adventurerlock = new object();
        readonly List<Adventure> adventurers = new List<Adventure>();
        readonly HashSet<long> activeplayers=new HashSet<long>();

        readonly ExplorationLogic explorationlogic;

        readonly List<Adventure> toprocess = new List<Adventure>();
         
        /// <summary>
        /// creates a new <see cref="AdventureModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public AdventureModule(StreamModule stream, PlayerModule players, TimerModule timer) {
            this.stream = stream;
            this.players = players;
            explorationlogic = new ExplorationLogic(context);

            players.PlayerLevelUp += OnLevelUp;
            players.PlayerStatusChanged += OnStatusChanged;
            stream.ChatMessage += OnChatMessage;
            stream.Command += OnCommand;
            stream.RegisterCommandHandler("explore", new ExploreCommandHandler(this, players));
            stream.RegisterCommandHandler("rest", new RestCommandHandler(this, players));
            stream.RegisterCommandHandler("rescue", new RescuePlayerCommandHandler(this));
            timer.AddService(this, 0.5);
        }

        /// <summary>
        /// triggered when activity of a player has changed
        /// </summary>
        public event Action<long, bool> PlayerActiveChanged;

        /// <summary>
        /// triggered when a player does something
        /// </summary>
        public event Action<long> PlayerActiveTrigger;

        /// <summary>
        /// triggered when the status of a player has changed
        /// </summary>
        public event Action<long, AdventureStatus> PlayerStatusChanged;

        /// <summary>
        /// triggered when an item was found
        /// </summary>
        public event Action<long, long, int> ItemFound;

        public void TriggerItemFound(long player, long item, int quantity) {
            ItemFound?.Invoke(player, item, quantity);
        }

        /// <summary>
        /// determines whether someone is adventuring
        /// </summary>
        public bool IsSomeoneActive
        {
            get
            {
                lock(adventurerlock)
                    return activeplayers.Any();
            }
        }
        public IEnumerable<Adventure> Adventures
        {
            get
            {
                lock(adventurerlock) {
                    foreach(Adventure adventure in adventurers)
                        yield return adventure;
                }
            }
        }

        void OnChatMessage(IChatChannel channel, ChatMessage message) {
            if(message.IsWhisper || string.IsNullOrEmpty(message.Service))
                return;

            PlayerModule playermodule = context.GetModule<PlayerModule>();
            Player player = playermodule.GetPlayer(message.Service, message.User);
            PlayerActiveTrigger?.Invoke(player.UserID);
        }

        void OnCommand(StreamCommand command) {
            PlayerActiveTrigger?.Invoke(context.GetModule<UserModule>().GetExistingUser(command.Service, command.User).ID);
        }

        void OnStatusChanged(long playerid, bool isactive) {
            RemoveAdventurer(playerid);
        }

        void IRunnableModule.Stop() {
            context.GetModule<PlayerModule>().PlayerLevelUp -= OnLevelUp;
            context.GetModule<PlayerModule>().PlayerStatusChanged -= OnStatusChanged;
            context.GetModule<StreamModule>().ChatMessage -= OnChatMessage;
            context.GetModule<StreamModule>().Command -= OnCommand;
            context.GetModule<StreamModule>().UnregisterCommandHandler("explore");
            context.GetModule<StreamModule>().UnregisterCommandHandler("rest");
            context.GetModule<StreamModule>().UnregisterCommandHandler("rescue");
            context.GetModule<TimerModule>().RemoveService(this);
        }

        void OnLevelUp(long playerid) {
            lock(adventurerlock) {
                if (!activeplayers.Contains(playerid))
                    return;

                Adventure adventure = adventurers.FirstOrDefault(a => a.Player == playerid);
                if(adventure == null)
                    return;

                Player player = context.GetModule<PlayerModule>().GetPlayer(playerid);
                User user = context.GetModule<UserModule>().GetUser(playerid);
                context.GetModule<RPGMessageModule>().Create().User(user).Text(" reaches level ").Bold().Color(Color.Gold).Text(player.Level.ToString()).Reset().Text(".").Send();
            }
        }

        void ITimerService.Process(double time) {
            toprocess.Clear();
            lock(adventurerlock) {
                adventurers.ForEach(a => a.Cooldown -= time);
                toprocess.AddRange(adventurers.Where(a => a.Cooldown <= 0.0));

                foreach (Adventure adventure in toprocess)
                {
                    AdventureStatus newstatus = adventure.AdventureLogic.ProcessPlayer(adventure.Player);
                    ChangeStatus(adventure, newstatus);
                }
            }
        }

        public void ChangeStatus(Adventure adventure, AdventureStatus status, object argument=null) {
            if(adventure.AdventureLogic.Status != status) {
                switch(status) {
                    case AdventureStatus.Exploration:
                        adventure.AdventureLogic = explorationlogic;
                        break;
                    case AdventureStatus.MonsterBattle:
                        if(argument != null) {
                            MonsterBattleLogic battlelogic = new MonsterBattleLogic(context.GetModule<RPGMessageModule>());
                            battlelogic.Add(new PlayerBattleEntity(context, adventure.Player, adventure, battlelogic));
                            battlelogic.Add(new MonsterBattleEntity(context.GetModule<MonsterModule>().GetMonster((string)argument, context.GetModule<PlayerModule>().GetExistingPlayer(adventure.Player).Level), context, adventure, battlelogic));
                            adventure.AdventureLogic = battlelogic;
                        }
                        else {
                            MonsterBattleLogic battlelogic = new MonsterBattleLogic(context.GetModule<RPGMessageModule>());
                            battlelogic.Add(new PlayerBattleEntity(context, adventure.Player, adventure, battlelogic));
                            battlelogic.Add(new MonsterBattleEntity(context.GetModule<MonsterModule>().GetMonster(context.GetModule<PlayerModule>().GetExistingPlayer(adventure.Player).Level), context, adventure, battlelogic));
                            adventure.AdventureLogic = battlelogic;
                        }
                        break;
                    case AdventureStatus.SpiritRealm:
                        context.GetModule<EffectModule>().ClearPlayerEffects(adventure.Player);
                        adventure.AdventureLogic = new SpiritRealmLogic(context);
                        break;
                }
                PlayerStatusChanged?.Invoke(adventure.Player, status);
            }
            adventure.Reset();
        }

        /// <summary>
        /// sends a player on adventure
        /// </summary>
        /// <param name="player"></param>
        public void AddAdventurer(Player player) {
            lock(adventurerlock) {
                if(activeplayers.Contains(player.UserID))
                    return;

                Adventure adventure = new Adventure(player.UserID) {
                    AdventureLogic = player.CurrentHP > 0 ? (IAdventureLogic)explorationlogic : new SpiritRealmLogic(context)
                };
                adventure.Reset();

                adventurers.Add(adventure);
                activeplayers.Add(player.UserID);

                PlayerActiveChanged?.Invoke(player.UserID, true);
                context.GetModule<RPGMessageModule>().Create().User(player.UserID).Text(" went out to adventure.").Send();
            }
        }

        /// <summary>
        /// stops an adventure for a player
        /// </summary>
        /// <param name="playerid">id of player to remove</param>
        public void RemoveAdventurer(long playerid) {
            lock(adventurerlock) {
                activeplayers.Remove(playerid);
                if(adventurers.RemoveAll(p => p.Player == playerid) > 0) {
                    PlayerActiveChanged?.Invoke(playerid, false);
                    context.GetModule<RPGMessageModule>().Create().User(playerid).Text(" stopped adventuring.").Send();
                }
            }
        }

        public void StartAdventure(string service, string username) {
            AddAdventurer(context.GetModule<PlayerModule>().GetExistingPlayer(service, username));
        }

        public void StopAdventure(string service, string username) {
            RemoveAdventurer(context.GetModule<UserModule>().GetUserID(service, username));
        }

        public void Stimulate(string service, string username) {
            User user = context.GetModule<UserModule>().GetUser(service, username);
            lock (adventurerlock)
                adventurers.FirstOrDefault(a => a.Player==user.ID).Cooldown = 0.0;
        }

        public void ChangeStatus(string service, string username, AdventureStatus status) {
            User user = context.GetModule<UserModule>().GetUser(service, username);
            lock (adventurerlock)
                ChangeStatus(adventurers.FirstOrDefault(a => a.Player == user.ID), status);
        }

        /// <summary>
        /// takes a break from adventuring
        /// </summary>
        /// <param name="playerid">id of player to put to rest</param>
        public void Rest(long playerid) {
            RemoveAdventurer(playerid);
            User user = context.GetModule<UserModule>().GetUser(playerid);
            context.GetModule<RPGMessageModule>().Create().User(user).Text(" went to the city to rest a bit.").Send();
        }

        public void Rescue(string service, string channel, string username) {
            lock(adventurerlock) {
                User rescueuser = context.GetModule<UserModule>().GetExistingUser(service, username);
                Player rescueplayer = context.GetModule<PlayerModule>().GetPlayer(rescueuser.ID);

                if(rescueplayer.CurrentHP == 0) {
                    context.GetModule<StreamModule>().SendMessage(service, channel, username, "Umm ... you're dead, you know.");
                    return;
                }

                Adventure adventure = adventurers.FirstOrDefault(a => a.AdventureLogic.Status == AdventureStatus.SpiritRealm && a.Player != rescueplayer.UserID);
                if(adventure == null) {
                    context.GetModule<StreamModule>().SendMessage(service, channel, username, "There is no one to rescue out there.");
                    return;
                }

                
                Player player = context.GetModule<PlayerModule>().GetExistingPlayer(adventure.Player);
                User user = context.GetModule<UserModule>().GetUser(player.UserID);

                int gold = Math.Min(player.Gold, 20 * player.Level);

                context.GetModule<PlayerModule>().Revive(player.UserID);
                ChangeStatus(adventure, AdventureStatus.Exploration);

                if (gold > 0) {
                    context.GetModule<PlayerModule>().UpdateGold(player.UserID, -gold);
                    context.GetModule<PlayerModule>().UpdateGold(rescueplayer.UserID, gold);

                    context.GetModule<RPGMessageModule>().Create().User(rescueuser).Text(" rescued ").User(user).Text(" for ").Gold(gold).Text(".").Send();
                }
                else {
                    context.GetModule<PlayerModule>().UpdateGold(rescueplayer.UserID, 50);
                    context.GetModule<RPGMessageModule>().Create().User(rescueuser).Text(" rescued ").User(user).Text(" for ").Gold(50).Text(".").Send();
                }
            }
        }
    }
}