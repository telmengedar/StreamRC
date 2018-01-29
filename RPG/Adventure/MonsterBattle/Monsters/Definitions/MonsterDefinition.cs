namespace StreamRC.RPG.Adventure.MonsterBattle.Monsters.Definitions {

    /// <summary>
    /// definition for a monster
    /// </summary>
    public class MonsterDefinition {

        /// <summary>
        /// name of monster
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// level table
        /// </summary>
        public string LevelResource { get; set; }

        /// <summary>
        /// item drops
        /// </summary>
        public MonsterDrop[] DroppedItems { get; set; }

        /// <summary>
        /// skills monster can use
        /// </summary>
        public MonsterSkillRange[] SkillSet { get; set; }
    }
}