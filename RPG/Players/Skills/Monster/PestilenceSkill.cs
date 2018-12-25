using NightlyCode.Core.Randoms;
using NightlyCode.Math;
using NightlyCode.Modules;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Effects.Status;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Players.Skills.Monster {

    /// <summary>
    /// skill module which executes pestilence skill
    /// </summary>
    [Module(Key = "skill.pestilence")]
    public class PestilenceSkill : SkillExecutionModule {
        readonly IModuleContext context;
        readonly RPGMessageModule messages;

        public PestilenceSkill(IModuleContext context, RPGMessageModule messages) {
            this.context = context;
            this.messages = messages;
        }

        public override string Name => "Pestilence";

        int GetModifiedDexterity(IBattleEntity entity, int skillevel) {
            switch(skillevel) {
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
            float hitprobability = MathCore.Sigmoid(GetModifiedDexterity(attacker, skilllevel) - target.Dexterity, 1.1f, 0.5f);
            messages.Create().BattleActor(attacker).Text(" tries to bite ").BattleActor(target).Text(".").Send();

            if(RNG.XORShift64.NextFloat() < hitprobability)
                target.AddEffect(new PlaqueEffect(context, target, messages) {
                    Level = skilllevel,
                    Time = 60.0 + 120.0 * skilllevel
                });
        }
    }
}