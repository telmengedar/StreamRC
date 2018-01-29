using NightlyCode.DB.Entities.Attributes;
using StreamRC.RPG.Items;

namespace StreamRC.RPG.Shops {

    [View("StreamRC.RPG.Shops.adviseditem.sql")]
    public class AdvisedItem {
        /// <summary>
        /// id of item
        /// </summary>
        public long ItemID { get; set; }

        public long PlayerID { get; set; }

        public int Value { get; set; }

        /// <summary>
        /// name of item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// type of item
        /// </summary>
        public ItemType Type { get; set; }

        /// <summary>
        /// where to equip item
        /// </summary>
        public ItemEquipmentTarget Target { get; set; }

        /// <summary>
        /// level required to find the item
        /// </summary>
        public int LevelRequirement { get; set; }

        /// <summary>
        /// armor value
        /// </summary>
        public int Armor { get; set; }

        /// <summary>
        /// weapon damage
        /// </summary>
        public int Damage { get; set; }

        /// <summary>
        /// determines whether the item is countable
        /// </summary>
        [DefaultValue(true)]
        public bool Countable { get; set; }

        /// <summary>
        /// number of items on stock
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// amount of discount for item
        /// </summary>
        public double Discount { get; set; }

    }
}