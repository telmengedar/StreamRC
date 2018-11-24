using System;
using System.Linq;
using NightlyCode.Core.Conversion;
using NightlyCode.Database.Clients.Tables;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Messages;
using StreamRC.Core.Scripts;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Equipment {

    [Module]
    public class EquipmentModule {
        readonly DatabaseModule database;
        readonly RPGMessageModule messages;
        readonly StreamModule stream;
        readonly UserModule users;
        readonly PlayerModule players;
        readonly ItemModule items;
        readonly InventoryModule inventory;

        /// <summary>
        /// creates a new <see cref="EquipmentModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public EquipmentModule(DatabaseModule database, RPGMessageModule messages, StreamModule stream, UserModule users, PlayerModule players, ItemModule items, InventoryModule inventory) {
            database.Database.UpdateSchema<EquipmentItem>();
            database.Database.UpdateSchema<EquippedItemInformation>();

            this.database = database;
            this.messages = messages;
            this.stream = stream;
            this.users = users;
            this.players = players;
            this.items = items;
            this.inventory = inventory;
        }

        /// <summary>
        /// triggered when the equipment of a player slot has changed
        /// </summary>
        public event Action<long, EquipmentSlot> EquipmentChanged;

        [Command("equipment", "$service", "$channel", "$user")]
        public void ShowEquipment(string service, string channel, string username) {
            User user = users.GetExistingUser(service, username);
            EquippedItemInformation[] equipment = database.Database.LoadEntities<EquippedItemInformation>().Where(i => i.PlayerID == user.ID).Execute().ToArray();

            int armor = 0;
            int damage = 0;
            foreach(EquippedItemInformation item in equipment) {
                armor += item.Armor;
                damage += item.Damage;
            }

            stream.SendMessage(service, channel, username, $"Your equipment: {string.Join(", ", equipment.Select(i => i.ToString()))}. Totaling to +{damage} Damage and +{armor} Defense");
        }

        public EquipmentBonus GetEquipmentBonus(long playerid) {
            DataTable table = database.Database.Load<EquippedItemInformation>(i => DBFunction.Sum(i.Damage), i => DBFunction.Sum(i.Armor)).Where(i => i.PlayerID == playerid).Execute();
            return new EquipmentBonus {
                Damage = Converter.Convert<int>(table.Rows[0][0], true),
                Armor = Converter.Convert<int>(table.Rows[0][1], true),
                WeaponCritical = database.Database.Load<EquippedItemInformation>(i => DBFunction.Average(i.UsageOptimum)).Where(i => i.PlayerID == playerid && i.Type == ItemType.Armor).ExecuteScalar<int>(),
                ArmorCritical = database.Database.Load<EquippedItemInformation>(i => DBFunction.Average(i.UsageOptimum)).Where(i => i.PlayerID == playerid && i.Type == ItemType.Weapon).ExecuteScalar<int>()
            };
        }

        public EquipmentBonus GetEquipmentBonus(long playerid, EquipmentSlot slot)
        {            
            DataTable valuetable = database.Database.Load<EquippedItemInformation>(i => DBFunction.Sum(i.Damage), i => DBFunction.Sum(i.Armor)).Where(i => i.PlayerID == playerid && i.Slot == slot).Execute();
            
            return new EquipmentBonus
            {
                Damage = Converter.Convert<int>(valuetable.Rows[0][0], true),
                Armor = Converter.Convert<int>(valuetable.Rows[0][1], true),
            };
        }

        public EquipmentItem GetEquipment(long playerid, EquipmentSlot slot) {
            return database.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == playerid && i.Slot == slot).Execute().FirstOrDefault();
        }

        [Command("compare", "$service", "$channel", "$user")]
        public void Compare(string service, string channel, string user, string[] arguments)
        {
            Player player = players.GetExistingPlayer(service, user);
            if (player == null)
                throw new Exception($"Player for '{service}/{user}' does not exist.");

            Item item = items.GetItem(arguments);
            if (item == null)
            {
                stream.SendMessage(service, channel, user, $"Unknown item '{string.Join(" ", arguments)}'.");
                return;
            }

            EquipmentSlot slot = item.GetTargetSlot();
            if(slot==EquipmentSlot.None) {
                stream.SendMessage(service, channel, user, $"'{item.Name}' can not be equipped.");
                return;
            }

            
            EquipmentItem equipment = GetEquipment(player.UserID, slot);
            if(equipment == null) {
                stream.SendMessage(service, channel, user, $"You have nothing equipped on your '{slot}'. {item.Name} is most likely better than nothing.");
                return;
            }

            if(equipment.ItemID == item.ID) {
                stream.SendMessage(service, channel, user, $"You have already equipped {item.Name.GetPreposition()}{item.Name} on your '{slot}'.");
                return;
            }

            EquipmentBonus bonus = GetEquipmentBonus(player.UserID, slot);

            switch (item.Type) {
                case ItemType.Armor:
                    int armordifference = item.Armor - bonus.Armor;
                    if(armordifference > 0) {
                        stream.SendMessage(service, channel, user, $"{item.Name} prevents {armordifference} more damage in a fight.");
                    }
                    else if(armordifference < 0) {
                        stream.SendMessage(service, channel, user, $"{item.Name} prevents {-armordifference} less damage in a fight.");
                    }
                    else stream.SendMessage(service, channel, user, $"{item.Name} has exactly the same effect as your current equipment.");
                    break;
                case ItemType.Weapon:
                    int weapondifference = item.Damage - bonus.Damage;
                    if(weapondifference>0)
                        stream.SendMessage(service, channel, user, $"{item.Name} does {weapondifference} more damage in a fight.");
                    else if(weapondifference<0)
                        stream.SendMessage(service, channel, user, $"{item.Name} does {weapondifference} less damage in a fight.");
                    else stream.SendMessage(service, channel, user, $"{item.Name} has exactly the same effect as your current equipment.");
                    break;
            }
        }

        [Command("takeoff", "$service", "$channel", "$username", "{0}")]
        public void TakeOff(string service, string channel, string username, string itemorslotname) {
            User user = users.GetExistingUser(service, username);
            Player player = players.GetExistingPlayer(service, username);
            if (player == null)
                throw new Exception($"Player for '{service}/{username}' does not exist.");

            Item item = items.GetItem(itemorslotname);
            if (item == null) {
                EquipmentSlot slot;
                try
                {
                    slot = (EquipmentSlot)Enum.Parse(typeof(EquipmentSlot), itemorslotname, true);
                }
                catch (Exception) {
                    stream.SendMessage(service, channel, username, $"'{itemorslotname}' is not an item or slotname.");
                    return;
                }

                TakeOffSlot(service, channel, username, player.UserID, slot);
                return;
            }

            EquipmentItem currentequipment = database.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == player.UserID && i.ItemID==item.ID).Execute().FirstOrDefault();
            if(currentequipment == null) {
                stream.SendMessage(service, channel, username, $"You don't have equipped any '{item.Name}'.");
            }
            else {
                inventory.AddItem(player.UserID, item.ID, 1,  true);
                database.Database.Delete<EquipmentItem>().Where(i => i.PlayerID == player.UserID && i.Slot == currentequipment.Slot).Execute();

                messages.Create().User(user).Text(" took off the ").Item(item, 0).Text(" from the ").EquipmentSlot(currentequipment.Slot).Text(".").Send();
                EquipmentChanged?.Invoke(player.UserID, currentequipment.Slot);
            }
        }

        void TakeOffSlot(string service, string channel, string username, long playerid, EquipmentSlot slot) {
            EquipmentItem currentequipment = database.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == playerid && i.Slot == slot).Execute().FirstOrDefault();
            if(currentequipment == null) {
                stream.SendMessage(service, channel, username, $"UUh ... you have nothing equipped on '{slot}'.");
                return;
            }

            User user = users.GetExistingUser(service, username);
            Item item = items.GetItem(currentequipment.ItemID);

            database.Database.Delete<EquipmentItem>().Where(i => i.PlayerID == playerid && i.Slot == slot).Execute();
            inventory.AddItem(playerid, currentequipment.ItemID, 1, true);

            messages.Create().User(user).Text(" took off the ").Item(item, 0).Text(" from the ").EquipmentSlot(slot).Text(".").Send();

            EquipmentChanged?.Invoke(playerid, currentequipment.Slot);
        }

        /// <summary>
        /// equips an item for a player
        /// </summary>
        /// <param name="service">service user is connected to</param>
        /// <param name="username">user which owns player</param>
        /// <param name="player">player to equip an item to</param>
        /// <param name="item">item to equip</param>
        public void Equip(string service, string channel, string username, Player player, Item item) {
            if (player.Level < item.LevelRequirement)
            {
                stream.SendMessage(service, channel, username, $"You need to be level {item.LevelRequirement} to equip {item.Name}.");
                return;
            }

            InventoryItem inventoryitem = inventory.GetItem(player.UserID, item.ID);
            if (inventoryitem == null)
            {
                stream.SendMessage(service, channel, username, $"{item.Name} not found in your inventory.");
                return;
            }

            EquipmentSlot slot = item.GetTargetSlot();
            if (slot == EquipmentSlot.None)
            {
                stream.SendMessage(service, channel, username, $"You don't really know where to equip that {item.Name}.");
                return;
            }

            EquipmentItem currentequipment = database.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == player.UserID && i.Slot == slot).Execute().FirstOrDefault();
            if (item.ID == currentequipment?.ItemID)
            {
                stream.SendMessage(service, channel, username, $"So you want to exchange your {item.Name} with {item.GetEnumerationName()}? Think again ....");
                return;
            }

            User user = users.GetExistingUser(service, username);

            inventory.RemoveItem(player.UserID, item.ID, 1);
            if (currentequipment == null)
            {
                database.Database.Insert<EquipmentItem>().Columns(i => i.PlayerID, i => i.Slot, i => i.ItemID).Values(player.UserID, slot, item.ID).Execute();
                messages.Create().User(user).Text(" equipped ").Item(item).Text(" to the ").EquipmentSlot(slot).Text(".").Send();
            }
            else
            {
                Item currentitem = items.GetItem(currentequipment.ItemID);
                database.Database.Update<EquipmentItem>().Set(i => i.ItemID == item.ID).Where(i => i.PlayerID == player.UserID && i.Slot == slot).Execute();
                inventory.AddItem(player.UserID, currentequipment.ItemID, 1, true);
                messages.Create().User(user).Text(" took off ").Item(currentitem).Text(" and equipped ").Item(item).Text(" to the ").EquipmentSlot(slot).Text(".").Send();
            }

            EquipmentChanged?.Invoke(player.UserID, slot);
        }

        public EquipmentItem Equip(long playerid, Item item, EquipmentSlot slot) {
            return Equip(users.GetUser(playerid), item, slot);
        }

        public EquipmentItem Equip(User user, Item item, EquipmentSlot slot) {
            inventory.RemoveItem(user.ID, item.ID, 1);

            EquipmentItem currentequipment = database.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == user.ID && i.Slot == slot).Execute().FirstOrDefault();
            if (currentequipment == null)
            {
                database.Database.Insert<EquipmentItem>().Columns(i => i.PlayerID, i => i.Slot, i => i.ItemID).Values(user.ID, slot, item.ID).Execute();
                messages.Create().User(user).Text(" equipped ").Item(item).Text(" to the ").EquipmentSlot(slot).Text(".").Send();
            }
            else
            {
                Item currentitem = items.GetItem(currentequipment.ItemID);
                database.Database.Update<EquipmentItem>().Set(i => i.ItemID == item.ID).Where(i => i.PlayerID == user.ID && i.Slot == slot).Execute();
                inventory.AddItem(user.ID, currentequipment.ItemID, 1, true);
                messages.Create().User(user).Text(" took off ").Item(currentitem).Text(" and equipped ").Item(item).Text(" to the ").EquipmentSlot(slot).Text(".").Send();
            }

            EquipmentChanged?.Invoke(user.ID, slot);
            return currentequipment;
        }

        /// <summary>
        /// equips an item of the inventory
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">username</param>
        /// <param name="itemname">name of item to equip</param>
        [Command("equip", "$service", "$channel", "$user")]
        public void Equip(string service, string channel, string user, string itemname) {
            Player player = players.GetExistingPlayer(service, user);
            if(player == null)
                throw new Exception($"Player for '{service}/{user}' does not exist.");

            Item item = items.GetItem(itemname);
            if(item == null) {
                stream.SendMessage(service, channel, user, $"Unknown item '{itemname}'.");
                return;

            }

            Equip(service, channel, user, player, item);
        }
    }
}