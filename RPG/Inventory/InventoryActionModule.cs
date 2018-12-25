using System.Linq;
using NightlyCode.Modules;
using StreamRC.Core.Scripts;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Equipment;
using StreamRC.RPG.Items;
using StreamRC.RPG.Players;
using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Inventory {

    [Module]
    public class InventoryActionModule {
        readonly StreamModule stream;
        readonly ItemModule items;
        readonly EquipmentModule equipment;
        readonly PlayerModule players;
        readonly SkillModule skills;
        readonly EffectModule effects;
        readonly UserModule users;
        readonly InventoryModule inventory;

        /// <summary>
        /// creates a new <see cref="InventoryActionModule"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="items"></param>
        /// <param name="equipment"></param>
        /// <param name="players"></param>
        /// <param name="skills"></param>
        /// <param name="effects"></param>
        /// <param name="users"></param>
        /// <param name="inventory"></param>
        public InventoryActionModule(StreamModule stream, ItemModule items, EquipmentModule equipment, PlayerModule players, SkillModule skills, EffectModule effects, UserModule users, InventoryModule inventory) {
            this.stream = stream;
            this.items = items;
            this.equipment = equipment;
            this.players = players;
            this.skills = skills;
            this.effects = effects;
            this.users = users;
            this.inventory = inventory;
        }

        [Command("use", "$service", "$channel", "$user")]
        public void UseItem(string service, string channel, string username, string[] arguments)
        {
            if (arguments.Length == 0)
            {
                stream.SendMessage(service, channel, username, "Well, you have to specify the name of the item to use");
                return;
            }

            int index = 0;
            Item item = items.RecognizeItem(arguments, ref index);
            if (item == null)
            {
                stream.SendMessage(service, channel, username, $"An item of the name '{string.Join(" ", arguments)}' is unknown.");
                return;
            }

            if (item.Type == ItemType.Armor || item.Type == ItemType.Weapon)
            {

                equipment.Equip(service, channel, username, players.GetPlayer(service, username), item);
                return;
            }

            if (item.Type != ItemType.Consumable && item.Type != ItemType.Potion && string.IsNullOrEmpty(item.Command))
            {
                stream.SendMessage(service, channel, username, $"Yeah sure ... use {item.Name} ... get real!");
                return;
            }

            User user = users.GetExistingUser(service, username);
            Player player = players.GetPlayer(user.ID);
            if (player == null)
            {
                stream.SendMessage(service, channel, username, "Umm ... you do not seem to be a player in this channel.");
                return;
            }

            skills.ModifyPlayerStats(player);
            effects.ModifyPlayerStats(player);

            inventory.UseItem(channel, user, player, item, arguments.Skip(index).ToArray());
        }

    }
}