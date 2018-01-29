namespace StreamRC.RPG.Items {

    /// <summary>
    /// type of item
    /// </summary>
    public enum ItemType {

        /// <summary>
        /// gold used to exchange items in shop or pay for whatever services
        /// </summary>
        Gold,

        /// <summary>
        /// item used to attack enemies
        /// </summary>
        Weapon,

        /// <summary>
        /// item used to protect agains attacks
        /// </summary>
        Armor,

        Consumable,

        Misc,

        /// <summary>
        /// special item (not to be sold)
        /// </summary>
        Special,

        /// <summary>
        /// special kind of consumable
        /// </summary>
        Potion,

        /// <summary>
        /// book to be read
        /// </summary>
        Book
    }
}