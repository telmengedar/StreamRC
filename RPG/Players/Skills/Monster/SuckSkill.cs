using System;
using NightlyCode.Core.Randoms;
using NightlyCode.Math;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Players.Skills.Monster {

    public class SuckSkill : MonsterSkill {
        readonly RPGMessageModule messages;

        public SuckSkill(int level, RPGMessageModule messages) {
            this.messages = messages;
            Level = level;
        }

        public override string Name => "Suck";

        public override int Level { get; }

        int GetModifiedDexterity(IBattleEntity entity) {
            switch(Level) {
                case 1:
                    return (int)(entity.Dexterity * 0.35);
                case 2:
                    return (int)(entity.Dexterity * 0.45);
                case 3:
                    return (int)(entity.Dexterity * 0.6);
                default:
                    return entity.Dexterity;
            }
        }

        public override void Process(IBattleEntity attacker, IBattleEntity target) {
            float hitprobability = MathCore.Sigmoid(GetModifiedDexterity(attacker) - target.Dexterity, 1.1f, 0.68f);
            RPGMessageBuilder message = messages.Create().BattleActor(attacker).Text(" tries to bite ").BattleActor(target);

            if(RNG.XORShift64.NextFloat() < hitprobability) {

                int hp = Math.Min(target.HP, (int)(target.MaxHP * (0.1 + 0.05 * Level)));
                message.Text(", sucks on him and heals").Health(attacker.Heal(hp)).Text(".");
                target.Hit(hp);
            }
            else message.Text(" but fails miserably.");
            message.Send();
        }
    }
}