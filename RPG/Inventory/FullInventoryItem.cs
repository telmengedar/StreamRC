using NightlyCode.DB.Entities.Attributes;
using StreamRC.RPG.Items;

namespace StreamRC.RPG.Inventory {

    /// <summary>
    /// inventory item with item name
    /// </summary>
    [View("StreamRC.RPG.Inventory.Views.fullinventoryitem.sql")]
    public class FullInventoryItem {

        /// <summary>
        /// id of item
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// id of player
        /// </summary>
        public long PlayerID { get; set; }

        /// <summary>
        /// name of item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// type of item
        /// </summary>
        public ItemType Type { get; set; }

        /// <summary>
        /// item count
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// number of hitpoints healed on usage
        /// </summary>
        public int HP { get; set; }

        /// <summary>
        /// value of item
        /// </summary>
        public int Value { get; set; }
    }
}