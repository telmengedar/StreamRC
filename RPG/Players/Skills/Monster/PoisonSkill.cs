using NightlyCode.Core.Randoms;
using NightlyCode.Math;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Effects.Status;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Players.Skills.Monster {
    public class PoisonSkill : MonsterSkill {
        readonly Context context;

        public PoisonSkill(Context context, int level) {
            Level = level;
            this.context = context;
        }

        public override string Name => "Poison";

        public override int Level { get; }

        int GetModifiedDexterity(IBattleEntity entity) {
            switch(Level) {
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

        public override void Process(IBattleEntity attacker, IBattleEntity target) {
            float hitprobability = MathCore.Sigmoid(GetModifiedDexterity(attacker) - target.Dexterity, 1.1f, 0.7f);
            context.GetModule<RPGMessageModule>().Create().BattleActor(attacker).Text(" tries to bite ").BattleActor(target).Text(".").Send();
            
            if(RNG.XORShift64.NextFloat() < hitprobability)
                target.AddEffect(new PoisonEffect(target, context.GetModule<AdventureModule>(), context.GetModule<RPGMessageModule>()) {
                    Level = Level,
                    Time = 30.0 + 160.0 * Level
                });
        }
    }
}