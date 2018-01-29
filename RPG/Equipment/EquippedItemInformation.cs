using NightlyCode.DB.Entities.Attributes;
using StreamRC.RPG.Items;

namespace StreamRC.RPG.Equipment {

    /// <summary>
    /// information about equipped item
    /// </summary>
    [View("StreamRC.RPG.Equipment.equippediteminformation.sql")]
    public class EquippedItemInformation {

        /// <summary>
        /// id of player
        /// </summary>
        public long PlayerID { get; set; }

        /// <summary>
        /// id of item
        /// </summary>
        public long ItemID { get; set; }

        /// <summary>
        /// name of equipped item
        /// </summary>
        public string Name { get; set; }

       /// <summary>
       /// type of equipped item
       /// </summary>
        public ItemType Type { get; set; }

        /// <summary>
        /// target of equipment
        /// </summary>
        public EquipmentSlot Slot { get; set; }

        /// <summary>
        /// armor value
        /// </summary>
        public int Armor { get; set; }

        /// <summary>
        /// weapon damage
        /// </summary>
        public int Damage { get; set; }

        /// <summary>
        /// optimum of usage
        /// </summary>
        public int UsageOptimum { get; set; }

        public override string ToString() {
            return $"{Slot} - {Name}(+{(Type == ItemType.Weapon ? Damage : Armor)})";
        }
    }
}