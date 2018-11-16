using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Effects.Modifiers {
    public class RockEffect : IModifierEffect {
        User user;
        readonly RPGMessageModule messages;

        public RockEffect(int level, double time, User user, RPGMessageModule messages) {
            Time = time;
            this.user = user;
            this.messages = messages;
            Level = level;
        }

        public string Name => "Rock";

        public void Initialize() {
            messages.Create().User(user).Text(" feels resistant like a rock.").Send();
        }

        public void WearOff() {
            messages.Create().User(user).Text(" is getting soft again.");
        }

        public double Time { get; set; }

        public int Level { get; set; }

        public void ModifyStats(Player player) {
            player.Fitness += (int)(Level * 2.23);
        }
    }
}