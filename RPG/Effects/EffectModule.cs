using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Timer;
using StreamRC.RPG.Effects.Battle;
using StreamRC.RPG.Effects.Modifiers;
using StreamRC.RPG.Effects.Status;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Effects {

    [Dependency(nameof(TimerModule), DependencyType.Type)]
    [Dependency(nameof(PlayerModule), DependencyType.Type)]
    [ModuleKey("effects")]
    public class EffectModule : IRunnableModule, ITimerService, IStreamCommandHandler, ICommandModule {
        readonly Context context;
        readonly object effectlock = new object();
        readonly List<ITemporaryEffect> monstereffects = new List<ITemporaryEffect>();
        readonly Dictionary<long, List<ITemporaryEffect>> playereffects = new Dictionary<long, List<ITemporaryEffect>>();

        public EffectModule(Context context) {
            this.context = context;
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

        void IStreamCommandHandler.ProcessStreamCommand(StreamCommand command) {
            switch(command.Command) {
                case "effects":
                    ListEffects(command.Service, command.User, command.IsWhispered);
                    break;
                default:
                    throw new StreamCommandException($"'{command.Command}' not handled by this module");
            }
        }

        void ListEffects(string service, string user, bool whispered) {
            string effects = string.Join(", ", GetActivePlayerEffects(context.GetModule<PlayerModule>().GetExistingPlayer(service, user).UserID));
            if(string.IsNullOrEmpty(effects))
                context.GetModule<StreamModule>().SendMessage(service, user, "No active effects", whispered);
            else context.GetModule<StreamModule>().SendMessage(service, user, $"Active effects: {effects}", whispered);
        }

        string IStreamCommandHandler.ProvideHelp(string command) {
            switch(command) {
                case "effects":
                    return "Lists all active effects on rpg player";
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module");
            }
        }

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().RegisterCommandHandler(this, "effects");
            context.GetModule<TimerModule>().AddService(this, 1.0);
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().UnregisterCommandHandler(this);
            context.GetModule<TimerModule>().RemoveService(this);
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
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, username);
            lock(effectlock) {
                ITemporaryEffect activeeffect = GetActivePlayerEffects(player.UserID).FirstOrDefault(e => e.Name.ToLower() == effect);
                if(activeeffect != null) {
                    if(level >= activeeffect.Level)
                        RemovePlayerEffect(player, activeeffect);
                    else {
                        activeeffect.Level -= level;
                        context.GetModule<RPGMessageModule>().Create().Text("The effect level of ").User(user).Text("'s ").Text(activeeffect.Name).Text(" was reduced to ").Text(activeeffect.Level.ToString()).Text(".").Send();
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
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            switch(effecttype) {
                case "smellyarmor":
                    AddEffect(service, username, new SmellyArmorEffect(level, time, context.GetModule<RPGMessageModule>(), user.ID));
                    break;
                case "shittyweapon":
                    AddEffect(service, username, new ShittyWeaponEffect(context, level, time, user.ID));
                    break;
                case "herakles":
                    AddEffect(service, username, new HeraklesEffect(level, time, context.GetModule<RPGMessageModule>(), user));
                    break;
                case "cat":
                    AddEffect(service, username, new CatEffect(level, time, context.GetModule<RPGMessageModule>(), user));
                    break;
                case "rock":
                    AddEffect(service, username, new RockEffect(level, time, context, user));
                    break;
                case "enlightment":
                    AddEffect(service, username, new EnlighmentEffect(level, time, context.GetModule<RPGMessageModule>(), user));
                    break;
                case "fortuna":
                    AddEffect(service, username, new FortunaEffect(level, time, context.GetModule<RPGMessageModule>(), user));
                    break;
            }
        }

        void AddEffect(string service, string user, ITemporaryEffect effect) {
            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, user);
            AddPlayerEffect(player.UserID, effect);
        }

        public void ClearPlayerEffects(long playerid) {
            lock(effectlock)
                playereffects.Remove(playerid);
        }

        public void RemoveEffect(ITemporaryEffect oldeffects) {
            lock(effectlock)
                monstereffects.RemoveAll(e => e == oldeffects);
        }
    }
}