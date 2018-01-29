using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Effects.Modifiers {
    public class CatEffect : IModifierEffect {
        readonly RPGMessageModule messages;
        readonly User user;

        public CatEffect(int level, double time, RPGMessageModule messages, User user) {
            Time = time;
            this.messages = messages;
            this.user = user;
            Level = level;
        }

        public string Name => "Cat";

        public void Initialize() {
            messages.Create().User(user).Text(" feels as graceful as a cat.").Send();
        }

        public void WearOff() {
            messages.Create().User(user).Text(" is endowed with clumsyness again.").Send();
        }

        public double Time { get; set; }
        public int Level { get; set; }

        public void ModifyStats(Player player) {
            player.Dexterity += (int)(Level * 2.73);
        }
    }
}