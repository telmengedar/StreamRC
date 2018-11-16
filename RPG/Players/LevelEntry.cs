using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.RPG.Players {

    public class LevelEntry {

        /// <summary>
        /// level for which entry defines data
        /// </summary>
        [Unique]
        public int Level { get; set; }

        public int Experience { get; set; }

        public int Health { get; set; }

        public int Mana { get; set; }

        public int Stamina { get; set; }

        public int Strength { get; set; }

        public int Intelligence { get; set; }

        public int Dexterity { get; set; }

        public int Fitness { get; set; }

        public int Luck { get; set; }
    }
}