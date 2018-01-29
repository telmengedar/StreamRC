using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Effects.Modifiers {
    public class FortunaEffect : IModifierEffect {
        RPGMessageModule messages;
        User user;

        public FortunaEffect(int level, double time, RPGMessageModule messages, User user) {
            Time = time;
            this.messages = messages;
            this.user = user;
            Level = level;
        }

        public string Name => "Fortuna";

        public void Initialize() {
            messages.Create().User(user).Text(" has seen Fortuna throwing kisses.").Send();            
        }

        public void WearOff() {
            messages.Create().User(user).Text(" sees a black cat talking to a raven through a broken mirror.").Send();
        }

        public double Time { get; set; }
        public int Level { get; set; }

        public void ModifyStats(Player player) {
            player.Luck += Level * 8;
        }
    }
}