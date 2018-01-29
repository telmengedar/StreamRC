using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Effects.Modifiers {
    public class EnlighmentEffect : IModifierEffect {
        readonly RPGMessageModule messages;
        readonly User user;

        public EnlighmentEffect(int level, double time, RPGMessageModule messages, User user) {
            Time = time;
            this.messages = messages;
            this.user = user;
            Level = level;
        }

        public string Name => "Enlightement";

        public void Initialize() {
            messages.Create().User(user).Text(" is getting ideas about all and everything.").Send();
        }

        public void WearOff() {
            messages.Create().User(user).Text(" can't tell triangles from squares anymore.").Send();
        }

        public double Time { get; set; }
        public int Level { get; set; }

        public void ModifyStats(Player player) {
            player.Intelligence += (int)(Level * 3.4);
        }
    }
}