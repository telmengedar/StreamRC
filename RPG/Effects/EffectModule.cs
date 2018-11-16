using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NightlyCode.Modules;
using StreamRC.Core.Timer;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Effects.Battle;
using StreamRC.RPG.Effects.Commands;
using StreamRC.RPG.Effects.Modifiers;
using StreamRC.RPG.Effects.Status;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Effects {

    [Module(Key="effects")]
    public class EffectModule : ITimerService, ICommandModule, IItemCommandModule {
        readonly StreamModule stream;
        readonly RPGMessageModule messages;
        readonly UserModule users;
        readonly PlayerModule players;
        readonly AdventureModule adventure;
        readonly object effectlock = new object();
        readonly List<ITemporaryEffect> monstereffects = new List<ITemporaryEffect>();
        readonly Dictionary<long, List<ITemporaryEffect>> playereffects = new Dictionary<long, List<ITemporaryEffect>>();

        public EffectModule(StreamModule stream, TimerModule timer, RPGMessageModule messages, UserModule users, PlayerModule players, AdventureModule adventure) {
            this.stream = stream;
            this.messages = messages;
            this.users = users;
            this.players = players;
            this.adventure = adventure;
            stream.RegisterCommandHandler("effects", new ListEffectsCommandHandler(this));
            timer.AddService(this, 1.0);
        }

        void ITimerService.Process(double time) {
            lock(effectlock) {
                for(int i = monstereffects.Count - 1; i >= 0; --i) {
                    ITemporaryEffect effect = monstereffects[i];
                    effect.Time -= time;
                    if(effect.Time <= 0.0) {
                        effect.WearOff();
                        monstereffects.RemoveAt(i);
                    }
                    else {
                        (effect as IStatusEffect)?.ProcessStatusEffect(time);
                    }
                }

                foreach(long player in playereffects.Keys.ToArray()) {
                    List<ITemporaryEffect> effects = playereffects[player];
                    for (int i = effects.Count - 1; i >= 0; --i) {
                        ITemporaryEffect effect = effects[i];
                        effect.Time -= time;
                        if(effect.Time <= 0.0) {
                            effect.WearOff();
                            effects.RemoveAt(i);
                        }
                        else {
                            (effect as IStatusEffect)?.ProcessStatusEffect(time);
                        }
                    }
                    if(effects.Count == 0)
                        playereffects.Remove(player);
                }
            }
        }

        /// <summary>
        /// adds an active monster effect
        /// </summary>
        /// <param name="effect"></param>
        public void AddMonsterEffect(ITemporaryEffect effect) {
            lock(effectlock) {
                monstereffects.Add(effect);
                effect.Initialize();
            }
        }

        /// <summary>
        /// adds an active player effect
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="effect">effect to add</param>
        public void AddPlayerEffect(long playerid, ITemporaryEffect effect) {
            lock(effectlock) {
                List<ITemporaryEffect> effects;
                if(!playereffects.TryGetValue(playerid, out effects))
                    playereffects[playerid] = effects = new List<ITemporaryEffect>();

                effects.RemoveAll(e => e.GetType() == effect.GetType());
                effects.Add(effect);
                effect.Initialize();
            }
        }

        public void ModifyPlayerStats(Player player) {
            foreach(IModifierEffect effect in GetActivePlayerEffects(player.UserID).Where(e=>e is IModifierEffect).Cast<IModifierEffect>())
                effect.ModifyStats(player);
        }

        /// <summary>
        /// get active effects for a player
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <returns>enumeration of active effects</returns>
        public IEnumerable<ITemporaryEffect> GetActivePlayerEffects(long playerid) {
            lock(effectlock) {
                List<ITemporaryEffect> effects;
                if(!playereffects.TryGetValue(playerid, out effects))
                    yield break;

                foreach(ITemporaryEffect effect in effects)
                    yield return effect;
            }
        }

        public void ListEffects(string service, string channel, string user) {
            string effects = string.Join(", ", GetActivePlayerEffects(players.GetExistingPlayer(service, user).UserID));
            if(string.IsNullOrEmpty(effects))
                stream.SendMessage(service, channel, user, "No active effects");
            else stream.SendMessage(service, channel, user, $"Active effects: {effects}");
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "add":
                    AddEffect(arguments[0], arguments[1], arguments[2], int.Parse(arguments[3]), double.Parse(arguments[4], CultureInfo.InvariantCulture));
                    break;
                case "reduce":
                    ReduceEffect(arguments[0], arguments[1], arguments[2], int.Parse(arguments[3]));
                    break;
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module");
            }
        }

        void ReduceEffect(string service, string username, string effect, int level) {
            User user = users.GetExistingUser(service, username);
            Player player = players.GetExistingPlayer(service, username);
            ReduceEffect(user, player, effect, level);
        }

        void ReduceEffect(User user, Player player, string effect, int level) {
            lock(effectlock) {
                ITemporaryEffect activeeffect = GetActivePlayerEffects(player.UserID).FirstOrDefault(e => e.Name.ToLower() == effect);
                if(activeeffect != null) {
                    if(level >= activeeffect.Level)
                        RemovePlayerEffect(player, activeeffect);
                    else {
                        activeeffect.Level -= level;
                        messages.Create().Text("The effect level of ").User(user).Text("'s ").Text(activeeffect.Name).Text(" was reduced to ").Text(activeeffect.Level.ToString()).Text(".").Send();
                    }
                }
            }
        }

        void RemovePlayerEffect(Player player, ITemporaryEffect activeeffect) {
            ITemporaryEffect effect = playereffects[player.UserID].FirstOrDefault(e => e == activeeffect);
            if(effect == null)
                return;

            effect.WearOff();
            playereffects[player.UserID].Remove(effect);
        }

        void AddEffect(string service, string username, string effecttype, int level, double time) {
            User user = users.GetExistingUser(service, username);
            AddEffect(user, effecttype, level, time);
        }

        void AddEffect(User user, string effecttype, int level, double time) {
            switch(effecttype) {
                case "smellyarmor":
                    AddPlayerEffect(user.ID, new SmellyArmorEffect(level, time, messages, user.ID));
                    break;
                case "shittyweapon":
                    AddPlayerEffect(user.ID, new ShittyWeaponEffect(level, time, user.ID, messages, adventure));
                    break;
                case "herakles":
                    AddPlayerEffect(user.ID, new HeraklesEffect(level, time, messages, user));
                    break;
                case "cat":
                    AddPlayerEffect(user.ID, new CatEffect(level, time, messages, user));
                    break;
                case "rock":
                    AddPlayerEffect(user.ID, new RockEffect(level, time, user, messages));
                    break;
                case "enlightment":
                    AddPlayerEffect(user.ID, new EnlighmentEffect(level, time, messages, user));
                    break;
                case "fortuna":
                    AddPlayerEffect(user.ID, new FortunaEffect(level, time, messages, user));
                    break;
            }
        }

        public void ClearPlayerEffects(long playerid) {
            lock(effectlock)
                playereffects.Remove(playerid);
        }

        public void RemoveEffect(ITemporaryEffect oldeffects) {
            lock(effectlock)
                monstereffects.RemoveAll(e => e == oldeffects);
        }

        void IItemCommandModule.ExecuteItemCommand(User user, Player player, string command, params string[] arguments) {
            switch (command)
            {
                case "add":
                    AddEffect(user, arguments[0], int.Parse(arguments[1]), double.Parse(arguments[2], CultureInfo.InvariantCulture));
                    break;
                case "reduce":
                    ReduceEffect(user, player, arguments[0], int.Parse(arguments[1]));
                    break;
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module");
            }
        }
    }
}