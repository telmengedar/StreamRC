using System;
using NightlyCode.Core.Randoms;
using NightlyCode.Math;
using NightlyCode.Modules;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Players.Skills.Monster {

    [Module(Key = "skill.suck")]
    public class SuckSkill : SkillExecutionModule {
        readonly RPGMessageModule messages;

        public SuckSkill(RPGMessageModule messages) {
            this.messages = messages;
        }

        public override string Name => "Suck";

        int GetModifiedDexterity(IBattleEntity entity, int skilllevel) {
            switch(skilllevel) {
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

        public override void Process(IBattleEntity attacker, IBattleEntity target, int skilllevel) {
            float hitprobability = MathCore.Sigmoid(GetModifiedDexterity(attacker, skilllevel) - target.Dexterity, 1.1f, 0.68f);
            RPGMessageBuilder message = messages.Create().BattleActor(attacker).Text(" tries to bite ").BattleActor(target);

            if(RNG.XORShift64.NextFloat() < hitprobability) {

                int hp = Math.Min(target.HP, (int)(target.MaxHP * (0.1 + 0.05 * skilllevel)));
                message.Text(", sucks on him and heals").Health(attacker.Heal(hp)).Text(".");
                target.Hit(hp);
            }
            else message.Text(" but fails miserably.");
            message.Send();
        }
    }
}