using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NightlyCode.Modules;
using StreamRC.Core.Scripts;
using StreamRC.Core.Timer;
using StreamRC.RPG.Adventure.Exploration;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters;
using StreamRC.RPG.Adventure.SpiritRealm;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Equipment;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Adventure {

    [Module(Key="adventure")]
    public class AdventureModule : ITimerService {
        readonly IModuleContext context;
        readonly StreamModule stream;
        readonly PlayerModule players;
        readonly object adventurerlock = new object();
        readonly List<Adventure> adventurers = new List<Adventure>();
        readonly HashSet<long> activeplayers=new HashSet<long>();

        readonly UserModule usermodule;
        readonly RPGMessageModule messages;
        readonly MonsterModule monstermodule;
        readonly EffectModule effectmodule;
        readonly SkillModule skillmodule;
        readonly EquipmentModule equipmentmodule;
        readonly InventoryModule inventorymodule;
        readonly ItemModule itemmodule;

        readonly List<Adventure> toprocess = new List<Adventure>();
         
        /// <summary>
        /// creates a new <see cref="AdventureModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public AdventureModule(IModuleContext context, StreamModule stream, PlayerModule players, TimerModule timer, UserModule usermodule, RPGMessageModule messages, MonsterModule monstermodule, EffectModule effectmodule, SkillModule skillmodule, EquipmentModule equipmentmodule, InventoryModule inventorymodule, ItemModule itemmodule) {
            this.context = context;
            this.stream = stream;
            this.players = players;
            this.usermodule = usermodule;
            this.messages = messages;
            this.monstermodule = monstermodule;
            this.effectmodule = effectmodule;
            this.skillmodule = skillmodule;
            this.equipmentmodule = equipmentmodule;
            this.inventorymodule = inventorymodule;
            this.itemmodule = itemmodule;

            players.PlayerLevelUp += OnLevelUp;
            players.PlayerStatusChanged += OnStatusChanged;
            stream.ChatMessage += OnChatMessage;
            stream.Command += OnCommand;
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

            Player player = players.GetPlayer(message.Service, message.User);
            PlayerActiveTrigger?.Invoke(player.UserID);
        }

        void OnCommand(StreamCommand command) {
            PlayerActiveTrigger?.Invoke(usermodule.GetExistingUser(command.Service, command.User).ID);
        }

        void OnStatusChanged(long playerid, bool isactive) {
            RemoveAdventurer(playerid);
        }

        void OnLevelUp(long playerid) {
            lock(adventurerlock) {
                if (!activeplayers.Contains(playerid))
                    return;

                Adventure adventure = adventurers.FirstOrDefault(a => a.Player == playerid);
                if(adventure == null)
                    return;

                Player player = players.GetPlayer(playerid);
                User user = usermodule.GetUser(playerid);
                messages.Create().User(user).Text(" reaches level ").Bold().Color(Color.Gold).Text(player.Level.ToString()).Reset().Text(".").Send();
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
                        adventure.AdventureLogic = context.GetModule<ExplorationLogic>();
                        break;
                    case AdventureStatus.MonsterBattle:
                        if(argument != null) {
                            MonsterBattleLogic battlelogic = new MonsterBattleLogic(context, messages);
                            battlelogic.Add(new PlayerBattleEntity(adventure.Player, adventure, battlelogic,usermodule,effectmodule,players,skillmodule,equipmentmodule,itemmodule,inventorymodule));
                            battlelogic.Add(new MonsterBattleEntity(monstermodule.GetMonster((string)argument, players.GetExistingPlayer(adventure.Player).Level), adventure, battlelogic, effectmodule, skillmodule));
                            adventure.AdventureLogic = battlelogic;
                        }
                        else {
                            MonsterBattleLogic battlelogic = new MonsterBattleLogic(context, messages);
                            battlelogic.Add(new PlayerBattleEntity(adventure.Player, adventure, battlelogic, usermodule, effectmodule, players, skillmodule, equipmentmodule, itemmodule, inventorymodule));
                            battlelogic.Add(new MonsterBattleEntity(monstermodule.GetMonster(players.GetExistingPlayer(adventure.Player).Level), adventure, battlelogic, effectmodule, skillmodule));
                            adventure.AdventureLogic = battlelogic;
                        }
                        break;
                    case AdventureStatus.SpiritRealm:
                        effectmodule.ClearPlayerEffects(adventure.Player);
                        adventure.AdventureLogic = new SpiritRealmLogic(players, usermodule, messages);
                        break;
                }
                PlayerStatusChanged?.Invoke(adventure.Player, status);
            }
            adventure.Reset();
        }

        [Command("explore", "$service", "$user")]
        public void AddAdventurer(string service, string user) {
            AddAdventurer(players.GetPlayer(service, user));
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
                    AdventureLogic = player.CurrentHP > 0 ? (IAdventureLogic)context.GetModule<ExplorationLogic>() : new SpiritRealmLogic(players, usermodule, messages)
                };
                adventure.Reset();

                adventurers.Add(adventure);
                activeplayers.Add(player.UserID);

                PlayerActiveChanged?.Invoke(player.UserID, true);
                messages.Create().User(player.UserID).Text(" went out to adventure.").Send();
            }
        }

        [Command("rest", "$service", "$user")]
        public void RemoveAdventurer(string service, string user) {
            RemoveAdventurer(usermodule.GetUserID(service, user));
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
                    messages.Create().User(playerid).Text(" stopped adventuring.").Send();
                }
            }
        }

        public void StartAdventure(string service, string username) {
            AddAdventurer(players.GetExistingPlayer(service, username));
        }

        public void StopAdventure(string service, string username) {
            RemoveAdventurer(usermodule.GetUserID(service, username));
        }

        public void Stimulate(string service, string username) {
            User user = usermodule.GetUser(service, username);
            lock (adventurerlock)
                adventurers.FirstOrDefault(a => a.Player==user.ID).Cooldown = 0.0;
        }

        public void ChangeStatus(string service, string username, AdventureStatus status) {
            User user = usermodule.GetUser(service, username);
            lock (adventurerlock)
                ChangeStatus(adventurers.FirstOrDefault(a => a.Player == user.ID), status);
        }

        /// <summary>
        /// takes a break from adventuring
        /// </summary>
        /// <param name="playerid">id of player to put to rest</param>
        public void Rest(long playerid) {
            RemoveAdventurer(playerid);
            User user = usermodule.GetUser(playerid);
            messages.Create().User(user).Text(" went to the city to rest a bit.").Send();
        }

        [Command("rescue", "$service", "$channel", "$user")]
        public void Rescue(string service, string channel, string username) {
            lock(adventurerlock) {
                User rescueuser = usermodule.GetExistingUser(service, username);
                Player rescueplayer = players.GetPlayer(rescueuser.ID);

                if(rescueplayer.CurrentHP == 0) {
                    stream.SendMessage(service, channel, username, "Umm ... you're dead, you know.");
                    return;
                }

                Adventure adventure = adventurers.FirstOrDefault(a => a.AdventureLogic.Status == AdventureStatus.SpiritRealm && a.Player != rescueplayer.UserID);
                if(adventure == null) {
                    stream.SendMessage(service, channel, username, "There is no one to rescue out there.");
                    return;
                }

                
                Player player = players.GetExistingPlayer(adventure.Player);
                User user = usermodule.GetUser(player.UserID);

                int gold = Math.Min(player.Gold, 20 * player.Level);

                players.Revive(player.UserID);
                ChangeStatus(adventure, AdventureStatus.Exploration);

                if (gold > 0) {
                    players.UpdateGold(player.UserID, -gold);
                    players.UpdateGold(rescueplayer.UserID, gold);

                    messages.Create().User(rescueuser).Text(" rescued ").User(user).Text(" for ").Gold(gold).Text(".").Send();
                }
                else {
                    players.UpdateGold(rescueplayer.UserID, 50);
                    messages.Create().User(rescueuser).Text(" rescued ").User(user).Text(" for ").Gold(50).Text(".").Send();
                }
            }
        }
    }
}