using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Randoms;
using StreamRC.Core.Messages;
using StreamRC.RPG.Equipment;

namespace StreamRC.RPG.Items {
    public static class ItemExtensions {

        public static int RecognizeQuantity(this string[] arguments, ref int index) {
            if(index >= arguments.Length)
                return -1;

            if (arguments[index].All(char.IsDigit))
                return int.Parse(arguments[index++]);

            switch (arguments[index].ToLower())
            {
                case "one":
                case "an":
                case "some":
                case "the":
                    ++index;
                    return 1;
                case "a":
                    if (arguments.Length > 1 && arguments[1].ToLower() == "few")
                    {
                        index = 2;
                        return 3 + RNG.XORShift64.NextInt(5);
                    }

                    ++index;
                    return 1;
            }

            return -1;
        }

        /// <summary>
        /// get target slot for equipment type
        /// </summary>
        /// <param name="item">equipment item</param>
        /// <returns>slot where item can be equipped</returns>
        public static EquipmentSlot GetTargetSlot(this Item item) {
            return item.Target.GetTargetSlot();
        }

        /// <summary>
        /// get target slot for equipment type
        /// </summary>
        /// <param name="target">equipment target type</param>
        /// <returns>slot where item can be equipped</returns>
        public static EquipmentSlot GetTargetSlot(this ItemEquipmentTarget target) {
            switch (target)
            {
                case ItemEquipmentTarget.Arm:
                    return EquipmentSlot.Arm;
                case ItemEquipmentTarget.Back:
                    return EquipmentSlot.Back;
                case ItemEquipmentTarget.Body:
                    return EquipmentSlot.Body;
                case ItemEquipmentTarget.Finger:
                    return EquipmentSlot.Finger1;
                case ItemEquipmentTarget.Foot:
                    return EquipmentSlot.Foot;
                case ItemEquipmentTarget.Hands:
                    return EquipmentSlot.Hand;
                case ItemEquipmentTarget.Head:
                    return EquipmentSlot.Head;
                case ItemEquipmentTarget.Legs:
                    return EquipmentSlot.Legs;
                case ItemEquipmentTarget.Neck:
                    return EquipmentSlot.Neck;
                case ItemEquipmentTarget.Shoulder:
                    return EquipmentSlot.Shoulder;
                default:
                    return EquipmentSlot.None;
            }
        }

        public static string GetEnumerationName(this Item item, bool useadjective=false) {
            if(!useadjective)
                return $"{item.Name.GetPreposition(item.Countable)}{item.Name}";
            return $"{item.CriticalAdjective.GetPreposition(item.Countable)}{item.CriticalAdjective} {item.Name}";
        }

        public static string GetMultiple(this Item item) {
            if(!item.Countable || item.Name.EndsWith("s"))
                return item.Name;

            if(item.Name.EndsWith("y"))
                return item.Name.Substring(0, item.Name.Length - 1) + "ies";

            if(item.Name.EndsWith("ch"))
                return item.Name + "es";

            return item.Name + "s";
        }

        public static IEnumerable<string> GetPossibleSingular(this string name) {
            yield return name;

            if (!name.EndsWith("s"))
                yield break;

            yield return name.Substring(0, name.Length - 1);

            if (name.EndsWith("ches")) {
                yield return name.Substring(0, name.Length - 2);
                yield break;
            }

            if(name.EndsWith("ies"))
                yield return name.Substring(0, name.Length - 3) + "y";
        }

        public static string GetCountName(this Item item, int quantity) {
            if(quantity == 0)
                return "no " + item.GetMultiple();

            if(quantity == 1)
                return item.GetEnumerationName();

            return quantity + " " + item.GetMultiple();
        }

        public static string GetSingularOrMultiple(this Item item, int quantity) {
            if(quantity > 1)
                return item.GetMultiple();
            return item.Name;
        }
    }
}