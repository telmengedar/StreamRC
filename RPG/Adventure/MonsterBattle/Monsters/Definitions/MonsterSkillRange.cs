namespace StreamRC.RPG.Adventure.MonsterBattle.Monsters.Definitions {

    /// <summary>
    /// range in monster levels where a skill is used
    /// </summary>
    public class MonsterSkillRange {

        /// <summary>
        /// minimum level of monster where this skill can be used
        /// </summary>
        public int MinLevel { get; set; }

        /// <summary>
        /// maximum level of monster where this skill can be used
        /// </summary>
        public int MaxLevel { get; set; }

        /// <summary>
        /// skill to be used
        /// </summary>
        public MonsterSkillDefinition Skill { get; set; } 
    }
}