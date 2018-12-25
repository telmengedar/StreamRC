using NightlyCode.Core.Randoms;
using NightlyCode.Math;
using NightlyCode.Modules;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Effects.Status;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Players.Skills.Monster {

    [Module(Key = "skill.poison")]
    public class PoisonSkill : SkillExecutionModule {
        readonly IModuleContext context;
        readonly RPGMessageModule messages;

        public PoisonSkill(IModuleContext context, RPGMessageModule messages) {
            this.context = context;
            this.messages = messages;
        }

        public override string Name => "Poison";

        int GetModifiedDexterity(IBattleEntity entity, int skilllevel) {
            switch(skilllevel) {
                case 1:
                    return (int)(entity.Dexterity * 0.4);
                case 2:
                    return (int)(entity.Dexterity * 0.56);
                case 3:
                    return (int)(entity.Dexterity * 0.68);
                default:
                    return entity.Dexterity;
            }
        }

        public override void Process(IBattleEntity attacker, IBattleEntity target, int skilllevel) {
            float hitprobability = MathCore.Sigmoid(GetModifiedDexterity(attacker, skilllevel) - target.Dexterity, 1.1f, 0.7f);
            messages.Create().BattleActor(attacker).Text(" tries to bite ").BattleActor(target).Text(".").Send();
            
            if(RNG.XORShift64.NextFloat() < hitprobability)
                target.AddEffect(new PoisonEffect(context, target, messages) {
                    Level = skilllevel,
                    Time = 30.0 + 160.0 * skilllevel
                });
        }
    }
}