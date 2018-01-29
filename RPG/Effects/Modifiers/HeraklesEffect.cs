using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Effects.Modifiers {
    public class HeraklesEffect : IModifierEffect {
        readonly RPGMessageModule messages;
        readonly User user;

        public HeraklesEffect(int level, double time, RPGMessageModule messages, User user) {
            Time = time;
            this.messages = messages;
            this.user = user;
            Level = level;
        }

        public string Name => "Herakles";

        public void Initialize() {
            messages.Create().User(user).Text(" is growing some muscles.").Send();
        }

        public void WearOff() {
            messages.Create().User(user).Text(" feels as weak as usual.").Send();
        }

        public double Time { get; set; }

        public int Level { get; set; }

        public void ModifyStats(Player player) {
            player.Strength += Level * 2;
        }
    }
}