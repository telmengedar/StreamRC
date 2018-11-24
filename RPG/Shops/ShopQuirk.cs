using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.RPG.Shops {
    public class ShopQuirk {

        [Index("quirk")]
        public long ID { get; set; }

        [Index("quirk")]
        public ShopQuirkType Type { get; set; } 
    }
}