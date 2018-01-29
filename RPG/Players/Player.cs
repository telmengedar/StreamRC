using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.RPG.Players {

    /// <summary>
    /// basic playerdata
    /// </summary>
    public class Player {

        /// <summary>
        /// id of user
        /// </summary>
        [Unique]
        public long UserID { get; set; }

        /// <summary>
        /// player level
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// collected gold
        /// </summary>
        public int Gold { get; set; }

        /// <summary>
        /// user experience
        /// </summary>
        public float Experience { get; set; }

        /// <summary>
        /// current health of player
        /// </summary>
        public int CurrentHP { get; set; }

        /// <summary>
        /// current mana of player
        /// </summary>
        public int CurrentMP { get; set; }

        /// <summary>
        /// maximum health of player
        /// </summary>
        public int MaximumHP { get; set; }

        /// <summary>
        /// maximum mana of player
        /// </summary>
        public int MaximumMP { get; set; }

        /// <summary>
        /// hit power
        /// </summary>
        public int Strength { get; set; }

        /// <summary>
        /// hit rate
        /// </summary>
        public int Dexterity { get; set; }

        /// <summary>
        /// power to cast spells
        /// </summary>
        public int Intelligence { get; set; }

        /// <summary>
        /// how much stamina recovers
        /// </summary>
        public int Fitness { get; set; }

        /// <summary>
        /// find something and good stuff
        /// </summary>
        public int Luck { get; set; }

        /// <summary>
        /// amount of pee stored
        /// </summary>
        [DefaultValue(0)]
        public int Pee { get; set; }

        /// <summary>
        /// amount of poo stored
        /// </summary>
        [DefaultValue(0)]
        public int Poo { get; set; }

        /// <summary>
        /// amount of vomit stored
        /// </summary>
        [DefaultValue(0)]
        public int Vomit { get; set; }

        /// <summary>
        /// determines whether player is active
        /// </summary>
        [DefaultValue(false)]
        public bool IsActive { get; set; }
    }
}