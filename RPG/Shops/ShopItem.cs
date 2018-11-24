using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.RPG.Shops {

    /// <summary>
    /// item in shop
    /// </summary>
    public class ShopItem {

        /// <summary>
        /// id of item
        /// </summary>
        [Index("item")]
        public long ItemID { get; set; }

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