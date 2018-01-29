namespace StreamRC.RPG.Adventure {

    /// <summary>
    /// possible event in an adventure
    /// </summary>
    public enum AdventureEvent {

        /// <summary>
        /// nothing happened
        /// </summary>
        Nothing,

        /// <summary>
        /// an item is found
        /// </summary>
        Item,

        /// <summary>
        /// a chest with items
        /// </summary>
        Chest,

        /// <summary>
        /// battle against a monster
        /// </summary>
        Monster
    }
}