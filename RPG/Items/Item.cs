using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.RPG.Items {

    /// <summary>
    /// item found in rpg
    /// </summary>
    public class Item {

        [PrimaryKey]
        [AutoIncrement]
        public long ID { get; set; }

        /// <summary>
        /// name of item
        /// </summary>
        [Unique]
        public string Name { get; set; }

        /// <summary>
        /// type of item
        /// </summary>
        public ItemType Type { get; set; }

        /// <summary>
        /// which handedness applies for armors or weapons
        /// </summary>
        public ItemHandedness Handedness { get; set; }

        /// <summary>
        /// where to equip item
        /// </summary>
        public ItemEquipmentTarget Target { get; set; }

        /// <summary>
        /// level required to find the item
        /// </summary>
        public int LevelRequirement { get; set; }

        /// <summary>
        /// luck required to find the item
        /// </summary>
        public int FindRequirement { get; set; }

        /// <summary>
        /// luck required to find the item all the time
        /// </summary>
        public int FindOptimum { get; set; }

        /// <summary>
        /// at which luck can the item be used optimally (critical damage/defend)
        /// </summary>
        public int UsageOptimum { get; set; }

        /// <summary>
        /// armor value
        /// </summary>
        public int Armor { get; set; }

        /// <summary>
        /// weapon damage
        /// </summary>
        public int Damage { get; set; }

        /// <summary>
        /// item value
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// regenerated health when consumed
        /// </summary>
        public int HP { get; set; }

        /// <summary>
        /// regenerated mana when consumed
        /// </summary>
        public int MP { get; set; }

        /// <summary>
        /// amount of pee generated
        /// </summary>
        [DefaultValue(0)]
        public int Pee { get; set; }

        /// <summary>
        /// amount of poo generated
        /// </summary>
        [DefaultValue(0)]
        public int Poo { get; set; }

        /// <summary>
        /// command to be executed when using the item
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// message to display when command is executed
        /// </summary>
        public string CommandMessage { get; set; }

        /// <summary>
        /// determines whether the item is countable
        /// </summary>
        [DefaultValue(true)]
        public bool Countable { get; set; }

        /// <summary>
        /// determines whether the item is a fluid
        /// </summary>
        [DefaultValue(false)]
        public bool IsFluid { get; set; }

        /// <summary>
        /// adjective to use on criticals
        /// </summary>
        public string CriticalAdjective { get; set; }

        protected bool Equals(Item other) {
            if(ID != 0 && other.ID != 0)
                return ID == other.ID;

            return string.Equals(Name, other.Name) && Type == other.Type && Handedness == other.Handedness && Target == other.Target && LevelRequirement == other.LevelRequirement && FindRequirement == other.FindRequirement && FindOptimum == other.FindOptimum && UsageOptimum == other.UsageOptimum && Armor == other.Armor && Damage == other.Damage && Value == other.Value && HP == other.HP && MP == other.MP && Command == other.Command && CommandMessage==other.CommandMessage && Countable == other.Countable && Pee==other.Pee && Poo==other.Poo && IsFluid==other.IsFluid && CriticalAdjective==other.CriticalAdjective;
        }

        public override bool Equals(object obj) {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != this.GetType()) return false;
            return Equals((Item)obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = Name?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ (int)Handedness;
                hashCode = (hashCode * 397) ^ (int)Target;
                hashCode = (hashCode * 397) ^ LevelRequirement;
                hashCode = (hashCode * 397) ^ FindRequirement;
                hashCode = (hashCode * 397) ^ FindOptimum;
                hashCode = (hashCode * 397) ^ UsageOptimum;
                hashCode = (hashCode * 397) ^ Armor;
                hashCode = (hashCode * 397) ^ Damage;
                hashCode = (hashCode * 397) ^ Value;
                hashCode = (hashCode * 397) ^ HP;
                hashCode = (hashCode * 397) ^ MP;
                hashCode = (hashCode * 397) ^ Poo;
                hashCode = (hashCode * 397) ^ Pee;
                hashCode = (hashCode * 397) ^ Command.GetHashCode();
                hashCode = (hashCode * 397) ^ CommandMessage.GetHashCode();
                hashCode = (hashCode * 397) ^ CriticalAdjective.GetHashCode();
                hashCode = (hashCode * 397) ^ Countable.GetHashCode();
                hashCode = (hashCode * 397) ^ IsFluid.GetHashCode();
                return hashCode;
            }
        }
    }
}