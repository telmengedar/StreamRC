using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Effects.Modifiers {
    public class RockEffect : IModifierEffect {
        Context context;
        User user;
        public RockEffect(int level, double time, Context context, User user) {
            Time = time;
            this.context = context;
            this.user = user;
            Level = level;
        }

        public string Name => "Rock";

        public void Initialize() {
            context.GetModule<RPGMessageModule>().Create().User(user).Text(" feels resistant like a rock.").Send();
        }

        public void WearOff() {
            context.GetModule<RPGMessageModule>().Create().User(user).Text(" is getting soft again.");
        }

        public double Time { get; set; }

        public int Level { get; set; }

        public void ModifyStats(Player player) {
            player.Fitness += (int)(Level * 2.23);
        }
    }
}