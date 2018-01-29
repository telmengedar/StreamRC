using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Core.Timer;
using StreamRC.RPG.Adventure.Exploration;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters;
using StreamRC.RPG.Adventure.SpiritRealm;
using StreamRC.RPG.Data;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Items;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Adventure {

    [Dependency(nameof(MessageModule), DependencyType.Type)]
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(PlayerModule), DependencyType.Type)]
    [Dependency(nameof(UserModule), DependencyType.Type)]
    [Dependency(nameof(TimerModule), DependencyType.Type)]
    [Dependency(nameof(ItemModule), DependencyType.Type)]
    [Dependency(nameof(MonsterModule), DependencyType.Type)]
    [Dependency(nameof(ImageCacheModule), DependencyType.Type)]
    [ModuleKey("adventure")]
    public class AdventureModule : IRunnableModule, ITimerService, ICommandModule, IStreamCommandHandler {
        readonly Context context;

        readonly object adventurerlock = new object();
        readonly List<Adventure> adventurers = new List<Adventure>();
        readonly HashSet<long> activeplayers=new HashSet<long>();

        readonly ExplorationLogic explorationlogic;

        readonly List<Adventure> toprocess = new List<Adventure>();
         
        /// <summary>
        /// creates a new <see cref="AdventureModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public AdventureModule(Context context) {
            this.context = context;
            explorationlogic = new ExplorationLogic(context);
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

        void OnChatMessage(ChatMessage message) {
            if(message.IsWhisper || string.IsNullOrEmpty(message.Service))
                return;

            PlayerModule playermodule = context.GetModule<PlayerModule>();
            Player player = playermodule.GetPlayer(message.Service, message.User);
            AddAdventurer(player);
            PlayerActiveTrigger?.Invoke(player.UserID);
        }

        void IRunnableModule.Start() {
            context.GetModule<PlayerModule>().PlayerLevelUp += OnLevelUp;
            context.GetModule<PlayerModule>().PlayerStatusChanged += OnStatusChanged;
            context.GetModule<StreamModule>().ChatMessage += OnChatMessage;
            context.GetModule<StreamModule>().Command += OnCommand;
            context.GetModule<StreamModule>().RegisterCommandHandler(this, "rescue", "rest");
            context.GetModule<TimerModule>().AddService(this, 0.5);
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
            context.GetModule<StreamModule>().UnregisterCommandHandler(this);
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
            }
        }

        /// <summary>
        /// stops an adventure for a player
        /// </summary>
        /// <param name="playerid">id of player to remove</param>
        public void RemoveAdventurer(long playerid) {
            lock(adventurerlock) {
                activeplayers.Remove(playerid);
                if(adventurers.RemoveAll(p => p.Player == playerid) > 0)
                    PlayerActiveChanged?.Invoke(playerid, false);
            }
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            PlayerModule playermodule = context.GetModule<PlayerModule>();
            switch (command) {
                case "start":
                    AddAdventurer(playermodule.GetExistingPlayer(arguments[0], arguments[1]));
                    break;
                case "stop":
                    RemoveAdventurer(playermodule.GetExistingPlayer(arguments[0], arguments[1]).UserID);
                    break;
                case "stimuli":
                    lock(adventurerlock)
                    adventurers.FirstOrDefault(a => {
                        User user = context.GetModule<UserModule>().GetUser(a.Player);
                        if(user.Service == arguments[0] && user.Name == arguments[1])
                            return true;
                        return false;
                    }).Cooldown = 0.0;
                    break;
                case "status":
                    lock(adventurerlock)
                        ChangeStatus(adventurers.FirstOrDefault(a => {
                            User user = context.GetModule<UserModule>().GetUser(a.Player);
                            if(user.Service == arguments[0] && user.Name == arguments[1])
                                return true;
                            return false;
                        }), (AdventureStatus)Enum.Parse(typeof(AdventureStatus), arguments[2], true), arguments.Length > 3 ? arguments[3] : null);
                    break;
            }
        }

        void IStreamCommandHandler.ProcessStreamCommand(StreamCommand command) {
            switch(command.Command) {
                case "rescue":
                    Rescue(command.Service, command.User);
                    break;
                case "rest":
                    RemoveAdventurer(context.GetModule<PlayerModule>().GetExistingPlayer(command.Service, command.User).UserID);
                    break;
            }
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

        void Rescue(string service, string username) {
            lock(adventurerlock) {
                User rescueuser = context.GetModule<UserModule>().GetExistingUser(service, username);
                Player rescueplayer = context.GetModule<PlayerModule>().GetPlayer(rescueuser.ID);

                if(rescueplayer.CurrentHP == 0) {
                    context.GetModule<StreamModule>().SendMessage(service, username, "Umm ... you're dead, you know.");
                    return;
                }

                Adventure adventure = adventurers.FirstOrDefault(a => a.AdventureLogic.Status == AdventureStatus.SpiritRealm && a.Player != rescueplayer.UserID);
                if(adventure == null) {
                    context.GetModule<StreamModule>().SendMessage(service, username, "There is no one to rescue out there.");
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

        string IStreamCommandHandler.ProvideHelp(string command) {
            switch(command) {
                case "rescue":
                    return "Rescues a dead player";
                case "rest":
                    return "Takes a rest from adventuring.";
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module.");

            }
        }
    }
}