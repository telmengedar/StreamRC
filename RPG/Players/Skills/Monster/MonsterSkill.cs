using StreamRC.Core.Messages;
using StreamRC.RPG.Adventure.MonsterBattle;

namespace StreamRC.RPG.Players.Skills.Monster {
    public abstract class MonsterSkill {
        public abstract string Name { get; }

        public abstract int Level { get; }

        public abstract void Process(IBattleEntity attacker, IBattleEntity target);
    }
}