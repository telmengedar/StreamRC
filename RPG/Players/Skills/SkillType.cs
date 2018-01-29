namespace StreamRC.RPG.Players.Skills {

    /// <summary>
    /// type of skills
    /// </summary>
    public enum SkillType {

        /// <summary>
        /// player has some basic ai for decisions
        /// </summary>
        Awareness=0x00000000,

        /// <summary>
        /// player is luckier than usual
        /// </summary>
        LuckyBastard=0x00000001,

        /// <summary>
        /// player can carry more than usual
        /// </summary>
        Mule = 0x00000002,

        /// <summary>
        /// heals hitpoints of a player
        /// </summary>
        Heal = 0x00000003,

        /// <summary>
        /// player gets better prices at shops
        /// </summary>
        Haggler = 0x00000004,

        /// <summary>
        /// monster (rat) tries to infect you with the plaque
        /// </summary>
        Pestilence=0x00010001,

        /// <summary>
        /// monster (bat) sucks blood from player to heal itself
        /// </summary>
        Suck=0x00010002,
    }
}