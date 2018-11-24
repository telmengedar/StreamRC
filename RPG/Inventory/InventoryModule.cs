using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Randoms;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Messages;
using StreamRC.Core.Scripts;
using StreamRC.RPG.Data;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Equipment;
using StreamRC.RPG.Items;
using StreamRC.RPG.Items.Recipes;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Inventory {

    /// <summary>
    /// module managing inventory of players
    /// </summary>
    [Module(Key="inventory", AutoCreate = true)]
    public class InventoryModule : ICommandModule {
        readonly DatabaseModule database;
        readonly StreamModule stream;
        readonly UserModule users;
        readonly PlayerModule players;
        readonly SkillModule skills;
        readonly ItemModule items;
        readonly RPGMessageModule messages;
        readonly EquipmentModule equipment;
        readonly EffectModule effects;

        /// <summary>
        /// creates a new <see cref="InventoryModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public InventoryModule(DatabaseModule database, StreamModule stream, UserModule users, PlayerModule players, SkillModule skills, ItemModule items, RPGMessageModule messages, EquipmentModule equipment, EffectModule effects) {
            this.database = database;
            this.stream = stream;
            this.users = users;
            this.players = players;
            this.skills = skills;
            this.items = items;
            this.messages = messages;
            this.equipment = equipment;
            this.effects = effects;
            database.Database.UpdateSchema<InventoryItem>();
            database.Database.UpdateSchema<FullInventoryItem>();
        }

        /// <summary>
        /// triggered when the player is encumbered
        /// </summary>
        public event Action<long> PlayerEncumbered;

        /// <summary>
        /// triggered when an item was added to the inventory
        /// </summary>
        public event Action<long, long, int> ItemAdded;

        public int GetMaximumInventorySize(long playerid) {
            int level = skills.GetSkill(playerid, SkillType.Mule)?.Level ?? 0;
            switch(level) {
                case 1:
                    return 16;
                case 2:
                    return 21;
                case 3:
                    return 25;
                default:
                    return 10;
            }
        }

        public bool HasItems(long playerid, long[] itemids) {
            return database.Database.Load<FullInventoryItem>(i=>DBFunction.Count).Where(i => i.PlayerID == playerid && i.Quantity > 0 && itemids.Contains(i.ID)).ExecuteScalar<int>() == itemids.Length;
        }

        public bool HasHealingItems(long playerid) {
            return database.Database.Load<FullInventoryItem>(i => DBFunction.Count).Where(i => i.PlayerID == playerid && i.HP > 0).ExecuteScalar<int>() > 0;
        }

        public FullInventoryItem[] GetInventoryItems(long playerid)
        {
            return database.Database.LoadEntities<FullInventoryItem>().Where(i => i.PlayerID == playerid).Execute().ToArray();
        }

        public FullInventoryItem[] GetInventoryItems(long playerid, ItemType itemtype) {
            return database.Database.LoadEntities<FullInventoryItem>().Where(i => i.PlayerID == playerid && i.Type == itemtype).Execute().ToArray();
        }

        /// <summary>
        /// adds an item to a player inventory
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="itemid">id of item to add</param>
        /// <param name="quantity">quantity to add</param>
        /// <param name="force">forces the item to be added ignoring whether the inventory is full</param>
        public AddInventoryItemResult AddItem(long playerid, long itemid, int quantity, bool force = false) {
            return AddItem(playerid, itemid, quantity, true, force);
        }

        AddInventoryItemResult AddItem(long playerid, long itemid, int quantity, bool encumberedevent, bool force) {
            if(itemid == 0)
                return AddInventoryItemResult.InvalidItem;

            if(database.Database.Update<InventoryItem>().Set(i => i.Quantity == i.Quantity + quantity).Where(i => i.PlayerID == playerid && i.ItemID == itemid).Execute() == 0) {
                if(force) {
                    database.Database.Insert<InventoryItem>().Columns(i => i.PlayerID, i => i.ItemID, i => i.Quantity).Values(playerid, itemid, quantity).Execute();
                    ItemAdded?.Invoke(playerid, itemid, quantity);
                    return AddInventoryItemResult.Success;
                }

                int maxsize = GetMaximumInventorySize(playerid);
                int inventorysize = database.Database.Load<InventoryItem>(i=>DBFunction.Count).Where(i => i.PlayerID == playerid).ExecuteScalar<int>();
                if (inventorysize < maxsize) {
                    database.Database.Insert<InventoryItem>().Columns(i => i.PlayerID, i => i.ItemID, i => i.Quantity).Values(playerid, itemid, quantity).Execute();
                    ItemAdded?.Invoke(playerid, itemid, quantity);
                    return inventorysize < maxsize - 1 ? AddInventoryItemResult.Success : AddInventoryItemResult.SuccessFull;
                }
                if(encumberedevent)
                    PlayerEncumbered?.Invoke(playerid);
                return AddInventoryItemResult.InventoryFull;
            }
            ItemAdded?.Invoke(playerid, itemid, quantity);
            return AddInventoryItemResult.Success;
        }

        /// <summary>
        /// removes an item quantity from player inventory
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="itemid">id of item</param>
        /// <param name="quantity">quantity to remove</param>
        public bool RemoveItem(long playerid, long itemid, int quantity) {
            bool result = database.Database.Update<InventoryItem>().Set(i => i.Quantity == i.Quantity - quantity).Where(i => i.PlayerID == playerid && i.ItemID == itemid).Execute() > 0;
            database.Database.Delete<InventoryItem>().Where(i => i.PlayerID == playerid && i.ItemID == itemid && i.Quantity <= 0).Execute();
            return result;
        }

        /// <summary>
        /// removes an item from player inventory
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="itemid">id of item</param>
        public bool RemoveItem(long playerid, long itemid)
        {
            return database.Database.Delete<InventoryItem>().Where(i => i.PlayerID == playerid && i.ItemID == itemid).Execute() > 0;
        }

        /// <summary>
        /// get current inventory size of a player
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <returns>number of items in player inventory</returns>
        public int GetInventorySize(long playerid) {
            return database.Database.Load<InventoryItem>(i=>DBFunction.Count).Where(i => i.PlayerID == playerid).ExecuteScalar<int>();
        }

        /// <summary>
        /// get item in inventory of a player
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="itemid">id of item</param>
        /// <returns>inventory instance of item for a player</returns>
        public InventoryItem GetItem(long playerid, long itemid) {
            return database.Database.LoadEntities<InventoryItem>().Where(i => i.PlayerID == playerid && i.ItemID == itemid).Execute().FirstOrDefault();
        }

        [Command("give", "$service", "$channel", "$user")]
        public void DonateItem(string service, string channel, string username, string[] arguments) {
            if(arguments.Length < 2) {
                stream.SendMessage(service, channel, username, "You have to tell me who to send what. Syntax: !donate <playername> [amount] <item>.");
                return;
            }

            User targetuser;
            try {
                targetuser = users.GetExistingUser(service, arguments[0].ToLower());
            }
            catch(Exception e) {
                Logger.Error(this, $"Error getting user '{arguments[0]}'", e);
                stream.SendMessage(service, channel, username, $"There is no player named '{arguments[0]}'.");
                return;
            }

            Player targetplayer = players.GetExistingPlayer(targetuser.ID);
            if (targetplayer == null)
            {
                stream.SendMessage(service, channel, username, $"'{arguments[0]}' has not yet played this game.");
                return;
            }

            int index = 1;
            int quantity=arguments.RecognizeQuantity(ref index);
            if(quantity == -1)
                quantity = 1;

            if(index >= arguments.Length) {
                stream.SendMessage(service, channel, username, "You have to tell me who to send what. Syntax: !donate <playername> [amount] <item>.");
                return;
            }

            Player player = players.GetExistingPlayer(service, username);
            User user = users.GetUser(player.UserID);

            Item item = items.RecognizeItem(arguments, ref index);
            if(item == null) {
                stream.SendMessage(service, channel, username, "I can't make out what item you want to donate.");
                return;
            }

            if(item.Type == ItemType.Gold) {
                if(quantity > player.Gold) {
                    stream.SendMessage(service, channel, username, "You don't have that much gold on you.");
                    return;
                }

                if(quantity <= 0) {
                    stream.SendMessage(service, channel, username, "Why do you tell me such weird nonsense?.");
                    return;
                }

                players.UpdateGold(player.UserID, -quantity);
                players.UpdateGold(targetplayer.UserID, quantity);
                messages.Create().User(user).Text(" has donated ").Gold(quantity).Text(" to ").User(targetuser).Text(".").Send();
            }
            else {
                if(!RemoveItem(player.UserID, item.ID)) {
                    stream.SendMessage(service, channel, username, $"And you are absolutely sure {item.GetEnumerationName()} is in your possession?.");
                    return;
                }

                switch(AddItem(targetplayer.UserID, item.ID, quantity)) {
                    case AddInventoryItemResult.InventoryFull:
                        AddItem(player.UserID, item.ID, quantity);
                        messages.Create().User(user).Text(" wanted to give ").Item(item, quantity).Text(" to ").User(targetuser).Text(". Sadly ").User(targetuser).Text(" is encumbered.").Send();
                        break;
                    default:
                        messages.Create().User(user).Text(" gave ").Item(item, quantity).Text(" to ").User(targetuser).Text(".").Send();
                        break;
                }
            }
        }

        [Command("craft", "$service", "$channel", "$user")]
        public void CraftItem(string service, string channel, string username, string[] arguments) {
            List<Item> ingredients = new List<Item>();
            int lastindex = 0;
            while(lastindex < arguments.Length) {
                bool found = false;
                for (int i = arguments.Length; i > lastindex; --i)
                {
                    Item item = items.GetItem(string.Join(" ", arguments.Skip(lastindex).Take(i - lastindex)));
                    if (item != null)
                    {
                        ingredients.Add(item);
                        lastindex = i;
                        found = true;
                        break;
                    }
                }
                if(!found)
                    break;
            }

            if (lastindex != arguments.Length) {
                stream.SendMessage(service, channel, username, "Not all of your ingredients are known so ... check spelling or whatever.");
                return;
            }

            if(ingredients.Count == 0) {
                stream.SendMessage(service, channel, username, "Your have to provide the names of the ingredients to use.");
                return;
            }

            Player player = players.GetExistingPlayer(service, username);

            if(ingredients.Any(i => GetItem(player.UserID, i.ID) == null)) {
                stream.SendMessage(service, channel, username, "It seems you don't have all of these ingredients in your inventory.");
                return;
            }

            CraftItem(player.UserID, ingredients.ToArray());
        }

        public void CraftItem(long playerid, Item[] ingredients) {
            ItemRecipe recipe = items.GetRecipe(ingredients.Select(i => i.ID).ToArray());

            if (recipe != null)
            {
                foreach (RecipeIngredient item in recipe.Ingredients.Where(i => i.Consumed))
                    RemoveItem(playerid, item.Item, 1);
            }
            else
            {
                foreach (Item item in ingredients)
                    RemoveItem(playerid, item.ID, 1);
            }

            User user = users.GetUser(playerid);
            Item targetitem = recipe != null ? items.GetItem(recipe.ItemID) : items.GetItem("Garbage");
            if(targetitem != null) {
                AddItem(playerid, targetitem.ID, 1, true);
                RPGMessageBuilder message = messages.Create();
                message.User(user).Text(" has created ").Item(targetitem).Text(" out of ");
                for(int i = 0; i < ingredients.Length; ++i) {
                    if(i == ingredients.Length - 1)
                        message.Text(" and ");
                    else if(i > 0)
                        message.Text(", ");
                    message.Item(ingredients[i]);
                }
                message.Text(".").Send();
            }
        }

        [Command("drop", "$service", "$channel", "$user")]
        public void DropItem(string service, string channel, string user, string[] arguments) {
            if (arguments.Length == 0)
            {
                stream.SendMessage(service, channel, user, "Well, you have to specify the name of the item to drop.");
                return;
            }

            Item item = items.GetItem(arguments);
            if (item == null)
            {
                stream.SendMessage(service, channel, user, $"An item of the name '{arguments[0]}' is unknown.");
                return;
            }

            Player player = players.GetPlayer(service, user);
            if (player == null)
            {
                stream.SendMessage(service, channel, user, "Umm ... you do not seem to be a player in this channel.");
                return;
            }

            if (!RemoveItem(player.UserID, item.ID))
            {
                stream.SendMessage(service, channel, user, $"And you are absolutely sure {item.Name.GetPreposition()}{item.Name} is in your possession?.");
                return;
            }

            stream.SendMessage(service, channel, user, $"You drop all {item.Name}s in your possession.");
        }

        public void UseItem(string channel, User user, Player player, Item item, params string[] arguments) {
            if(player.CurrentHP == 0) {
                stream.SendMessage(user.Service, channel, user.Name, "I am terribly sorry to inform you that you died quite a while ago.");
                return;
            }

            if (!RemoveItem(player.UserID, item.ID, 1))
            {
                stream.SendMessage(user.Service, channel, user.Name, $"And you are absolutely sure {item.GetEnumerationName()} is in your possession?.");
                return;
            }

            if (!string.IsNullOrEmpty(item.Command)) {
                string[] commandarguments = item.Command.Split(' ').ToArray();

                // TODO: implement item commands again using scripts
                object module = null;
                //IModule module = context.GetModuleByKey<IModule>(commandarguments[0]);
                if (module == null)
                {
                    Logger.Error(this, $"Invalid command in item {item.Name}.", $"Module '{commandarguments[0]}' not found.");
                }
                else if(!(module is IItemCommandModule) && !(module is ICommandModule)) {
                    Logger.Error(this, $"Invalid command in item {item.Name}.", $"Module '{commandarguments[0]}' is not able to execute commands.");
                }
                else
                {
                    try {
                        if(module is IItemCommandModule)
                            ((IItemCommandModule)module).ExecuteItemCommand(user, player, commandarguments[1], commandarguments.Skip(2).Concat(arguments).ToArray());
                        else
                            ((ICommandModule)module).ProcessCommand(commandarguments[1], new[] {user.Service, user.Name}.Concat(commandarguments.Skip(2)).Concat(arguments).ToArray());
                    }
                    catch(ItemUseException e) {
                        stream.SendMessage(user.Service, channel, user.Name, e.Message);
                        Logger.Warning(this, e.Message);
                        AddItem(player.UserID, item.ID, 1, true);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(this, $"Error executing item command of item '{item.Name}'.", e);
                        stream.SendMessage(user.Service, channel, user.Name, "Something went wrong when using the item.");
                        AddItem(player.UserID, item.ID, 1, true);
                        return;
                    }
                }
            }

            if(!string.IsNullOrEmpty(item.CommandMessage))
                new ItemUseContext(messages.Create(), user, item).Send(item.CommandMessage);

            if (item.Type != ItemType.Consumable && item.Type != ItemType.Potion)
                return;

            RPGMessageBuilder message = messages.Create();

            bool critical = RNG.XORShift64.NextFloat() < (float)player.Luck / Math.Max(1, item.UsageOptimum);
            int hp = Math.Min(item.HP * (critical ? 2 : 1), player.MaximumHP - player.CurrentHP);
            int mp = Math.Min(item.MP * (critical ? 2 : 1), player.MaximumMP - player.CurrentMP);

            message.User(user).Text(item.IsFluid ? " drinks " : " eats ").ItemUsage(item, critical);
            if (hp > 0 || mp > 0)
            {
                player.CurrentHP = Math.Min(player.CurrentHP + hp, player.MaximumHP);
                player.CurrentMP = Math.Min(player.CurrentMP + mp, player.MaximumMP);
                players.UpdateRunningStats(player.UserID, player.CurrentHP, player.CurrentMP);

                message.Text(" and regenerates ");
                if(hp > 0)
                    message.Color(AdventureColors.Health).Text($"{hp} HP").Reset();
                if(mp > 0) {
                    if(hp > 0)
                        message.Text(" and ");
                    message.Color(AdventureColors.Mana).Text($"{mp} MP").Reset();
                }
                message.Text(".");
            }
            else {
                message.Text(string.IsNullOrEmpty(item.Command) ? " for no reason." : ".");
            }

            player.Pee += item.Pee;
            player.Poo += item.Poo;
            player.Vomit += item.Pee + item.Poo;

            bool pee = player.Pee > 100;
            bool poo = player.Poo > 100;
            bool vomit = player.Vomit > 100;
            if (pee ||  poo || vomit) {
                message.Text(" Proudly ").User(user).Text(" produces ");
                if (player.Poo >= 100)
                {
                    player.Poo -= 100;
                    Item pooitem = items.GetItem("Poo");
                    message.Text(" a pile of ").Item(pooitem);
                    AddItem(player.UserID, pooitem.ID, 1, true);
                }

                if (player.Pee >= 100)
                {
                    if(poo)
                        message.Text(vomit ? ", " : " and ");

                    player.Pee -= 100;
                    Item peeitem = items.GetItem("Pee");
                    message.Text(" a puddle of ").Item(peeitem);
                    AddItem(player.UserID, peeitem.ID, 1, true);
                }

                if(player.Vomit >= 100) {
                    player.Vomit -= 100;
                    if(pee || poo)
                        message.Text(" and ");

                    Item vomititem = items.GetItem("Vomit");
                    message.Text(" an accumulation of finest ").Item(vomititem);
                    AddItem(player.UserID, vomititem.ID, 1, true);
                }

                message.Text(".");
            }
            message.Send();
            players.UpdatePeeAndPoo(player.UserID, player.Pee, player.Poo, player.Vomit);
        }

        [Command("use", "$service", "$channel", "$user")]
        public void UseItem(string service, string channel, string username, string[] arguments) {
            if (arguments.Length == 0) {
                stream.SendMessage(service, channel, username, "Well, you have to specify the name of the item to use");
                return;
            }

            int index = 0;
            Item item = items.RecognizeItem(arguments, ref index);
            if(item == null) {
                stream.SendMessage(service, channel, username, $"An item of the name '{string.Join(" ", arguments)}' is unknown.");
                return;
            }

            if(item.Type == ItemType.Armor || item.Type == ItemType.Weapon) {
                equipment.Equip(service, channel, username, players.GetPlayer(service, username), item);
                return;
            }

            if(item.Type != ItemType.Consumable && item.Type != ItemType.Potion && string.IsNullOrEmpty(item.Command)) {
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

            UseItem(channel, user, player, item, arguments.Skip(index).ToArray());
        }

        [Command("inventory", "$service", "$channel", "$user")]
        public void ShowInventory(string service, string channel, string user) {
            Player player = players.GetPlayer(service, user);
            stream.SendMessage(service, channel, user, string.Join(", ", database.Database.LoadEntities<FullInventoryItem>().Where(p=>p.PlayerID==player.UserID).Execute().Select(i=>$"{i.Quantity} x {i.Name}")));
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "add":
                    AddItem(players.GetExistingPlayer(arguments[0], arguments[1]).UserID, items.GetItemID(arguments.Skip(2).ToArray()), 1, true);
                    break;
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module.");
            }
        }
    }
}