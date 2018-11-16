using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.RPG.Inventory {

    /// <summary>
    /// item in player inventory
    /// </summary>
    public class InventoryItem {

        /// <summary>
        /// id of player
        /// </summary>
        [Index("item")]
        public long PlayerID { get; set; }

        /// <summary>
        /// id of item
        /// </summary>
        [Index("item")]
        public long ItemID { get; set; }

        /// <summary>
        /// quantity of item
        /// </summary>
        public int Quantity { get; set; }
    }
}