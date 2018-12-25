namespace StreamRC.RPG.Adventure.MonsterBattle.Monsters.Definitions {

    /// <summary>
    /// definition of a skill a monster can use
    /// </summary>
    public class SkillDefinition {

        /// <summary>
        /// name of skill
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// skill level
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// rate at which skill is used
        /// </summary>
        public double Rate { get; set; } 
    }
}