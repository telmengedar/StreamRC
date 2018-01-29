using System;
using System.Data;
using System.Linq;
using NightlyCode.Core.Conversion;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.RPG.Data;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Equipment {

    [Dependency(nameof(PlayerModule), DependencyType.Type)]
    [Dependency(nameof(ItemModule), DependencyType.Type)]
    [Dependency(nameof(InventoryModule), DependencyType.Type)]
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(GameMessageModule), DependencyType.Type)]
    public class EquipmentModule : IInitializableModule,IRunnableModule, IStreamCommandHandler {
        readonly Context context;

        RPGMessageModule messagemodule;

        /// <summary>
        /// creates a new <see cref="EquipmentModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public EquipmentModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// triggered when the equipment of a player slot has changed
        /// </summary>
        public event Action<long, EquipmentSlot> EquipmentChanged;

        void IStreamCommandHandler.ProcessStreamCommand(StreamCommand command) {
            switch(command.Command) {
                case "equip":
                    Equip(command.Service, command.User, string.Join(" ", command.Arguments));
                    break;
                case "equipment":
                    ShowEquipment(command.Service, command.User);
                    break;
                case "takeoff":
                    TakeOff(command.Service, command.User, string.Join(" ", command.Arguments));
                    break;
                case "compare":
                    Compare(command.Service, command.User, command.Arguments, command.IsWhispered);
                    break;
            }
        }

        void ShowEquipment(string service, string username) {
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            EquippedItemInformation[] equipment = context.Database.LoadEntities<EquippedItemInformation>().Where(i => i.PlayerID == user.ID).Execute().ToArray();

            int armor = 0;
            int damage = 0;
            foreach(EquippedItemInformation item in equipment) {
                armor += item.Armor;
                damage += item.Damage;
            }

            context.GetModule<StreamModule>().SendMessage(service, username, $"Your equipment: {string.Join(", ", equipment.Select(i => i.ToString()))}. Totaling to +{damage} Damage and +{armor} Defense");
        }

        string IStreamCommandHandler.ProvideHelp(string command) {
            switch(command) {
                case "equip":
                    return "Equips an item which is stored in your inventory. Syntax: !equip <itemname>";
                case "equipment":
                    return "Shows your current equipment.";
                case "takeoff":
                    return "Takes off an equipped item and puts it back in your inventory. !takeoff <itemname|slot>";
                case "compare":
                    return "Compares an item to the current equipment";
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module.");
            }
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<EquipmentItem>();
            context.Database.UpdateSchema<EquippedItemInformation>();

            context.GetModule<StreamModule>().RegisterCommandHandler(this, "equip", "equipment", "takeoff", "compare");
        }

        public EquipmentBonus GetEquipmentBonus(long playerid) {
            Aggregate damagesum = Aggregate.Sum<EquippedItemInformation>(EntityField.Create<EquippedItemInformation>(e => e.Damage));
            Aggregate armorsum = Aggregate.Sum<EquippedItemInformation>(EntityField.Create<EquippedItemInformation>(e => e.Armor));
            Aggregate usageoptimum = Aggregate.Average(EntityField.Create<EquippedItemInformation>(e => e.UsageOptimum));
            DataTable table = context.Database.Load<EquippedItemInformation>(i => damagesum, i => armorsum).Where(i => i.PlayerID == playerid).Execute();
            return new EquipmentBonus {
                Damage = Converter.Convert<int>(table.Rows[0][0], true),
                Armor = Converter.Convert<int>(table.Rows[0][1], true),
                WeaponCritical = context.Database.Load<EquippedItemInformation>(i => usageoptimum).Where(i => i.PlayerID == playerid && i.Type == ItemType.Armor).ExecuteScalar<int>(),
                ArmorCritical = context.Database.Load<EquippedItemInformation>(i => usageoptimum).Where(i => i.PlayerID == playerid && i.Type == ItemType.Weapon).ExecuteScalar<int>()
            };
        }

        public EquipmentBonus GetEquipmentBonus(long playerid, EquipmentSlot slot)
        {
            Aggregate damagesum = Aggregate.Sum<EquippedItemInformation>(EntityField.Create<EquippedItemInformation>(e => e.Damage));
            Aggregate armorsum = Aggregate.Sum<EquippedItemInformation>(EntityField.Create<EquippedItemInformation>(e => e.Armor));
            
            DataTable valuetable = context.Database.Load<EquippedItemInformation>(i => damagesum, i => armorsum).Where(i => i.PlayerID == playerid && i.Slot == slot).Execute();
            
            return new EquipmentBonus
            {
                Damage = Converter.Convert<int>(valuetable.Rows[0][0], true),
                Armor = Converter.Convert<int>(valuetable.Rows[0][1], true),
            };
        }

        public EquipmentItem GetEquipment(long playerid, EquipmentSlot slot) {
            return context.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == playerid && i.Slot == slot).Execute().FirstOrDefault();
        }

        void Compare(string service, string user, string[] arguments, bool isWhispered)
        {
            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, user);
            if (player == null)
                throw new Exception($"Player for '{service}/{user}' does not exist.");

            Item item = context.GetModule<ItemModule>().GetItem(arguments);
            if (item == null)
            {
                context.GetModule<StreamModule>().SendMessage(service, user, $"Unknown item '{string.Join(" ", arguments)}'.");
                return;
            }

            EquipmentSlot slot = item.GetTargetSlot();
            if(slot==EquipmentSlot.None) {
                context.GetModule<StreamModule>().SendMessage(service, user, $"'{item.Name}' can not be equipped.");
                return;
            }

            
            EquipmentItem equipment = GetEquipment(player.UserID, slot);
            if(equipment == null) {
                context.GetModule<StreamModule>().SendMessage(service, user, $"You have nothing equipped on your '{slot}'. {item.Name} is most likely better than nothing.");
                return;
            }

            if(equipment.ItemID == item.ID) {
                context.GetModule<StreamModule>().SendMessage(service, user, $"You have already equipped {item.Name.GetPreposition()}{item.Name} on your '{slot}'.");
                return;
            }

            EquipmentBonus bonus = GetEquipmentBonus(player.UserID, slot);

            switch (item.Type) {
                case ItemType.Armor:
                    int armordifference = item.Armor - bonus.Armor;
                    if(armordifference > 0) {
                        context.GetModule<StreamModule>().SendMessage(service, user, $"{item.Name} prevents {armordifference} more damage in a fight.");
                    }
                    else if(armordifference < 0) {
                        context.GetModule<StreamModule>().SendMessage(service, user, $"{item.Name} prevents {-armordifference} less damage in a fight.");
                    }
                    else context.GetModule<StreamModule>().SendMessage(service, user, $"{item.Name} has exactly the same effect as your current equipment.");
                    break;
                case ItemType.Weapon:
                    int weapondifference = item.Damage - bonus.Damage;
                    if(weapondifference>0)
                        context.GetModule<StreamModule>().SendMessage(service, user, $"{item.Name} does {weapondifference} more damage in a fight.");
                    else if(weapondifference<0)
                        context.GetModule<StreamModule>().SendMessage(service, user, $"{item.Name} does {weapondifference} less damage in a fight.");
                    else context.GetModule<StreamModule>().SendMessage(service, user, $"{item.Name} has exactly the same effect as your current equipment.");
                    break;
            }
        }

        public void TakeOff(string service, string username, string itemorslotname) {
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, username);
            if (player == null)
                throw new Exception($"Player for '{service}/{username}' does not exist.");

            Item item = context.GetModule<ItemModule>().GetItem(itemorslotname);
            if (item == null) {
                EquipmentSlot slot;
                try
                {
                    slot = (EquipmentSlot)Enum.Parse(typeof(EquipmentSlot), itemorslotname, true);
                }
                catch (Exception) {
                    context.GetModule<StreamModule>().SendMessage(service, username, $"'{itemorslotname}' is not an item or slotname.");
                    return;
                }

                TakeOffSlot(service, username, player.UserID, slot);
                return;
            }

            EquipmentItem currentequipment = context.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == player.UserID && i.ItemID==item.ID).Execute().FirstOrDefault();
            if(currentequipment == null) {
                context.GetModule<StreamModule>().SendMessage(service, username, $"You don't have equipped any '{item.Name}'.");
            }
            else {
                context.GetModule<InventoryModule>().AddItem(player.UserID, item.ID, 1,  true);
                context.Database.Delete<EquipmentItem>().Where(i => i.PlayerID == player.UserID && i.Slot == currentequipment.Slot).Execute();

                messagemodule.Create().User(user).Text(" took off the ").Item(item, 0).Text(" from the ").EquipmentSlot(currentequipment.Slot).Text(".").Send();
                EquipmentChanged?.Invoke(player.UserID, currentequipment.Slot);
            }
        }

        void TakeOffSlot(string service, string username, long playerid, EquipmentSlot slot) {
            EquipmentItem currentequipment = context.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == playerid && i.Slot == slot).Execute().FirstOrDefault();
            if(currentequipment == null) {
                context.GetModule<StreamModule>().SendMessage(service, username, $"UUh ... you have nothing equipped on '{slot}'.");
                return;
            }

            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            Item item = context.GetModule<ItemModule>().GetItem(currentequipment.ItemID);

            context.Database.Delete<EquipmentItem>().Where(i => i.PlayerID == playerid && i.Slot == slot).Execute();
            context.GetModule<InventoryModule>().AddItem(playerid, currentequipment.ItemID, 1, true);

            messagemodule.Create().User(user).Text(" took off the ").Item(item, 0).Text(" from the ").EquipmentSlot(slot).Text(".").Send();

            EquipmentChanged?.Invoke(playerid, currentequipment.Slot);
        }

        /// <summary>
        /// equips an item for a player
        /// </summary>
        /// <param name="service">service user is connected to</param>
        /// <param name="username">user which owns player</param>
        /// <param name="player">player to equip an item to</param>
        /// <param name="item">item to equip</param>
        public void Equip(string service, string username, Player player, Item item) {
            if (player.Level < item.LevelRequirement)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, $"You need to be level {item.LevelRequirement} to equip {item.Name}.");
                return;
            }

            InventoryItem inventoryitem = context.GetModule<InventoryModule>().GetItem(player.UserID, item.ID);
            if (inventoryitem == null)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, $"{item.Name} not found in your inventory.");
                return;
            }

            EquipmentSlot slot = item.GetTargetSlot();
            if (slot == EquipmentSlot.None)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, $"You don't really know where to equip that {item.Name}.");
                return;
            }

            EquipmentItem currentequipment = context.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == player.UserID && i.Slot == slot).Execute().FirstOrDefault();
            if (item.ID == currentequipment?.ItemID)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, $"So you want to exchange your {item.Name} with {item.GetEnumerationName()}? Think again ....");
                return;
            }

            User user = context.GetModule<UserModule>().GetExistingUser(service, username);

            context.GetModule<InventoryModule>().RemoveItem(player.UserID, item.ID, 1);
            if (currentequipment == null)
            {
                context.Database.Insert<EquipmentItem>().Columns(i => i.PlayerID, i => i.Slot, i => i.ItemID).Values(player.UserID, slot, item.ID).Execute();
                messagemodule.Create().User(user).Text(" equipped ").Item(item).Text(" to the ").EquipmentSlot(slot).Text(".").Send();
            }
            else
            {
                Item currentitem = context.GetModule<ItemModule>().GetItem(currentequipment.ItemID);
                context.Database.Update<EquipmentItem>().Set(i => i.ItemID == item.ID).Where(i => i.PlayerID == player.UserID && i.Slot == slot).Execute();
                context.GetModule<InventoryModule>().AddItem(player.UserID, currentequipment.ItemID, 1, true);
                messagemodule.Create().User(user).Text(" took off ").Item(currentitem).Text(" and equipped ").Item(item).Text(" to the ").EquipmentSlot(slot).Text(".").Send();
            }

            EquipmentChanged?.Invoke(player.UserID, slot);
        }

        public EquipmentItem Equip(long playerid, Item item, EquipmentSlot slot) {
            return Equip(context.GetModule<UserModule>().GetUser(playerid), item, slot);
        }

        public EquipmentItem Equip(User user, Item item, EquipmentSlot slot) {
            context.GetModule<InventoryModule>().RemoveItem(user.ID, item.ID, 1);

            EquipmentItem currentequipment = context.Database.LoadEntities<EquipmentItem>().Where(i => i.PlayerID == user.ID && i.Slot == slot).Execute().FirstOrDefault();
            if (currentequipment == null)
            {
                context.Database.Insert<EquipmentItem>().Columns(i => i.PlayerID, i => i.Slot, i => i.ItemID).Values(user.ID, slot, item.ID).Execute();
                messagemodule.Create().User(user).Text(" equipped ").Item(item).Text(" to the ").EquipmentSlot(slot).Text(".").Send();
            }
            else
            {
                Item currentitem = context.GetModule<ItemModule>().GetItem(currentequipment.ItemID);
                context.Database.Update<EquipmentItem>().Set(i => i.ItemID == item.ID).Where(i => i.PlayerID == user.ID && i.Slot == slot).Execute();
                context.GetModule<InventoryModule>().AddItem(user.ID, currentequipment.ItemID, 1, true);
                messagemodule.Create().User(user).Text(" took off ").Item(currentitem).Text(" and equipped ").Item(item).Text(" to the ").EquipmentSlot(slot).Text(".").Send();
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
        public void Equip(string service, string user, string itemname) {
            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, user);
            if(player == null)
                throw new Exception($"Player for '{service}/{user}' does not exist.");

            Item item = context.GetModule<ItemModule>().GetItem(itemname);
            if(item == null) {
                context.GetModule<StreamModule>().SendMessage(service, user, $"Unknown item '{itemname}'.");
                return;

            }

            Equip(service, user, player, item);
        }

        public void Start() {
            messagemodule = context.GetModule<RPGMessageModule>();
        }

        public void Stop() {
        }
    }
}