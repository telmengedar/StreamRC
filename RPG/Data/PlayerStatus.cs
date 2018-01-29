namespace StreamRC.RPG.Data {

    /// <summary>
    /// status of player
    /// </summary>
    public enum PlayerStatus {

        /// <summary>
        /// player is not in channel
        /// </summary>
        Absent=0,

        /// <summary>
        /// player is dead
        /// </summary>
        Dead=1,

        /// <summary>
        /// player is in channel but not doing anything
        /// </summary>
        Idle=2,

        /// <summary>
        /// player exploring
        /// </summary>
        Adventuring=3,
    }
}