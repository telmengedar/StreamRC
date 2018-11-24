using NightlyCode.Core.Randoms;
using NightlyCode.Math;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Effects.Status;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Players.Skills.Monster {
    public class PestilenceSkill : MonsterSkill {
        readonly AdventureModule adventure;
        readonly RPGMessageModule messages;

        public PestilenceSkill(int level, AdventureModule adventure, RPGMessageModule messages) {
            this.adventure = adventure;
            this.messages = messages;
            Level = level;
        }

        public override string Name => "Pestilence";

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
            float hitprobability = MathCore.Sigmoid(GetModifiedDexterity(attacker) - target.Dexterity, 1.1f, 0.5f);
            messages.Create().BattleActor(attacker).Text(" tries to bite ").BattleActor(target).Text(".").Send();
            
            if(RNG.XORShift64.NextFloat() < hitprobability)
                target.AddEffect(new PlaqueEffect(target, adventure, messages) {
                    Level = Level,
                    Time = 60.0 + 120.0 * Level
                });
        }
    }
}