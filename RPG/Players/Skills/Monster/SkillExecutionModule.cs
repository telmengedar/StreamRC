using StreamRC.Core.Messages;
using StreamRC.RPG.Adventure.MonsterBattle;

namespace StreamRC.RPG.Players.Skills.Monster {

    /// <summary>
    /// base module for a module which executes a skill
    /// </summary>
    public abstract class SkillExecutionModule {

        /// <summary>
        /// name of skill
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// executes the skill
        /// </summary>
        /// <param name="attacker">attacking entity</param>
        /// <param name="target">attack target</param>
        /// <param name="skilllevel">skill level</param>
        public abstract void Process(IBattleEntity attacker, IBattleEntity target, int skilllevel);
    }
}