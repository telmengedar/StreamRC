using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Randoms;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
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
    [Dependency(nameof(SkillModule), DependencyType.Type)]
    [Dependency(nameof(PlayerModule), DependencyType.Type)]
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(MessageModule), DependencyType.Type)]
    [ModuleKey("inventory")]
    public class InventoryModule : IInitializableModule, IStreamCommandHandler, ICommandModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="InventoryModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public InventoryModule(Context context) {
            this.context = context;
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
            int level = context.GetModule<SkillModule>().GetSkill(playerid, SkillType.Mule)?.Level ?? 0;
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
            return context.Database.Load<FullInventoryItem>(DBFunction.Count).Where(i => i.PlayerID == playerid && i.Quantity > 0 && itemids.Contains(i.ID)).ExecuteScalar<int>() == itemids.Length;
        }

        public bool HasHealingItems(long playerid) {
            return context.Database.Load<FullInventoryItem>(i => DBFunction.Count).Where(i => i.PlayerID == playerid && i.HP > 0).ExecuteScalar<int>() > 0;
        }

        public FullInventoryItem[] GetInventoryItems(long playerid)
        {
            return context.Database.LoadEntities<FullInventoryItem>().Where(i => i.PlayerID == playerid).Execute().ToArray();
        }

        public FullInventoryItem[] GetInventoryItems(long playerid, ItemType itemtype) {
            return context.Database.LoadEntities<FullInventoryItem>().Where(i => i.PlayerID == playerid && i.Type == itemtype).Execute().ToArray();
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

            if(context.Database.Update<InventoryItem>().Set(i => i.Quantity == i.Quantity + quantity).Where(i => i.PlayerID == playerid && i.ItemID == itemid).Execute() == 0) {
                if(force) {
                    context.Database.Insert<InventoryItem>().Columns(i => i.PlayerID, i => i.ItemID, i => i.Quantity).Values(playerid, itemid, quantity).Execute();
                    ItemAdded?.Invoke(playerid, itemid, quantity);
                    return AddInventoryItemResult.Success;
                }

                int maxsize = GetMaximumInventorySize(playerid);
                int inventorysize = context.Database.Load<InventoryItem>(DBFunction.Count).Where(i => i.PlayerID == playerid).ExecuteScalar<int>();
                if (inventorysize < maxsize) {
                    context.Database.Insert<InventoryItem>().Columns(i => i.PlayerID, i => i.ItemID, i => i.Quantity).Values(playerid, itemid, quantity).Execute();
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
            bool result = context.Database.Update<InventoryItem>().Set(i => i.Quantity == i.Quantity - quantity).Where(i => i.PlayerID == playerid && i.ItemID == itemid).Execute() > 0;
            context.Database.Delete<InventoryItem>().Where(i => i.PlayerID == playerid && i.ItemID == itemid && i.Quantity <= 0).Execute();
            return result;
        }

        /// <summary>
        /// removes an item from player inventory
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="itemid">id of item</param>
        public bool RemoveItem(long playerid, long itemid)
        {
            return context.Database.Delete<InventoryItem>().Where(i => i.PlayerID == playerid && i.ItemID == itemid).Execute() > 0;
        }

        /// <summary>
        /// get current inventory size of a player
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <returns>number of items in player inventory</returns>
        public int GetInventorySize(long playerid) {
            return context.Database.Load<InventoryItem>(DBFunction.Count).Where(i => i.PlayerID == playerid).ExecuteScalar<int>();
        }

        /// <summary>
        /// get item in inventory of a player
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="itemid">id of item</param>
        /// <returns>inventory instance of item for a player</returns>
        public InventoryItem GetItem(long playerid, long itemid) {
            return context.Database.LoadEntities<InventoryItem>().Where(i => i.PlayerID == playerid && i.ItemID == itemid).Execute().FirstOrDefault();
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<InventoryItem>();
            context.Database.UpdateSchema<FullInventoryItem>();
            context.GetModule<StreamModule>().RegisterCommandHandler(this, "inventory", "use", "drop", "craft", "give");
        }

        void IStreamCommandHandler.ProcessStreamCommand(StreamCommand command) {
            switch(command.Command) {
                case "inventory":
                    ShowInventory(command.Service, command.User, command.IsWhispered);
                    break;
                case "use":
                    UseItem(command.Service, command.User, command.Arguments, command.IsWhispered);
                    break;
                case "drop":
                    DropItem(command.Service, command.User, command.Arguments, command.IsWhispered);
                    break;
                case "craft":
                    CraftItem(command.Service, command.User, command.Arguments, command.IsWhispered);
                    break;
                case "give":
                    DonateItem(command.Service, command.User, command.Arguments, command.IsWhispered);
                    break;
                default:
                    throw new StreamCommandException($"Command {command.Command} not implemented by this module.");
            }
        }

        void DonateItem(string service, string username, string[] arguments, bool isWhispered) {
            if(arguments.Length < 2) {
                context.GetModule<StreamModule>().SendMessage(service, username, "You have to tell me who to send what. Syntax: !donate <playername> [amount] <item>.");
                return;
            }

            User targetuser;
            try {
                targetuser = context.GetModule<UserModule>().GetExistingUser(service, arguments[0].ToLower());
            }
            catch(Exception e) {
                Logger.Error(this, $"Error getting user '{arguments[0]}'", e);
                context.GetModule<StreamModule>().SendMessage(service, username, $"There is no player named '{arguments[0]}'.");
                return;
            }

            Player targetplayer = context.GetModule<PlayerModule>().GetExistingPlayer(targetuser.ID);
            if (targetplayer == null)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, $"'{arguments[0]}' has not yet played this game.");
                return;
            }

            int index = 1;
            int quantity=arguments.RecognizeQuantity(ref index);
            if(quantity == -1)
                quantity = 1;

            if(index >= arguments.Length) {
                context.GetModule<StreamModule>().SendMessage(service, username, "You have to tell me who to send what. Syntax: !donate <playername> [amount] <item>.");
                return;
            }

            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, username);
            User user = context.GetModule<UserModule>().GetUser(player.UserID);

            Item item = context.GetModule<ItemModule>().RecognizeItem(arguments, ref index);
            if(item == null) {
                context.GetModule<StreamModule>().SendMessage(service, username, "I can't make out what item you want to donate.");
                return;
            }

            if(item.Type == ItemType.Gold) {
                if(quantity > player.Gold) {
                    context.GetModule<StreamModule>().SendMessage(service, username, "You don't have that much gold on you.");
                    return;
                }

                if(quantity <= 0) {
                    context.GetModule<StreamModule>().SendMessage(service, username, "Why do you tell me such weird nonsense?.");
                    return;
                }

                context.GetModule<PlayerModule>().UpdateGold(player.UserID, -quantity);
                context.GetModule<PlayerModule>().UpdateGold(targetplayer.UserID, quantity);
                context.GetModule<RPGMessageModule>().Create().User(user).Text(" has donated ").Gold(quantity).Text(" to ").User(targetuser).Text(".").Send();
            }
            else {
                if(!RemoveItem(player.UserID, item.ID)) {
                    context.GetModule<StreamModule>().SendMessage(service, username, $"And you are absolutely sure {item.GetEnumerationName()} is in your possession?.");
                    return;
                }

                switch(AddItem(targetplayer.UserID, item.ID, quantity)) {
                    case AddInventoryItemResult.InventoryFull:
                        AddItem(player.UserID, item.ID, quantity);
                        context.GetModule<RPGMessageModule>().Create().User(user).Text(" wanted to give ").Item(item, quantity).Text(" to ").User(targetuser).Text(". Sadly ").User(targetuser).Text(" is encumbered.").Send();
                        break;
                    default:
                        context.GetModule<RPGMessageModule>().Create().User(user).Text(" gave ").Item(item, quantity).Text(" to ").User(targetuser).Text(".").Send();
                        break;
                }
            }
        }

        void CraftItem(string service, string username, string[] arguments, bool whispered) {
            List<Item> ingredients = new List<Item>();
            int lastindex = 0;
            while(lastindex < arguments.Length) {
                bool found = false;
                for (int i = arguments.Length; i > lastindex; --i)
                {
                    Item item = context.GetModule<ItemModule>().GetItem(string.Join(" ", arguments.Skip(lastindex).Take(i - lastindex)));
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
                context.GetModule<StreamModule>().SendMessage(service, username, "Not all of your ingredients are known so ... check spelling or whatever.");
                return;
            }

            if(ingredients.Count == 0) {
                context.GetModule<StreamModule>().SendMessage(service, username, "Your have to provide the names of the ingredients to use.");
                return;
            }

            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, username);

            if(ingredients.Any(i => GetItem(player.UserID, i.ID) == null)) {
                context.GetModule<StreamModule>().SendMessage(service, username, "It seems you don't have all of these ingredients in your inventory.");
                return;
            }

            CraftItem(player.UserID, ingredients.ToArray());
        }

        public void CraftItem(long playerid, Item[] ingredients) {
            ItemRecipe recipe = context.GetModule<ItemModule>().GetRecipe(ingredients.Select(i => i.ID).ToArray());

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

            User user = context.GetModule<UserModule>().GetUser(playerid);
            Item targetitem = recipe != null ? context.GetModule<ItemModule>().GetItem(recipe.ItemID) : context.GetModule<ItemModule>().GetItem("Garbage");
            if(targetitem != null) {
                AddItem(playerid, targetitem.ID, 1, true);
                RPGMessageBuilder message = context.GetModule<RPGMessageModule>().Create();
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

        void DropItem(string service, string user, string[] arguments, bool whispered) {
            if (arguments.Length == 0)
            {
                context.GetModule<StreamModule>().SendMessage(service, user, "Well, you have to specify the name of the item to drop.", whispered);
                return;
            }

            Item item = context.GetModule<ItemModule>().GetItem(arguments);
            if (item == null)
            {
                context.GetModule<StreamModule>().SendMessage(service, user, $"An item of the name '{arguments[0]}' is unknown.", whispered);
                return;
            }

            Player player = context.GetModule<PlayerModule>().GetPlayer(service, user);
            if (player == null)
            {
                context.GetModule<StreamModule>().SendMessage(service, user, "Umm ... you do not seem to be a player in this channel.", whispered);
                return;
            }

            if (!RemoveItem(player.UserID, item.ID))
            {
                context.GetModule<StreamModule>().SendMessage(service, user, $"And you are absolutely sure {item.Name.GetPreposition()}{item.Name} is in your possession?.", whispered);
                return;
            }

            context.GetModule<StreamModule>().SendMessage(service, user, $"You drop all {item.Name}s in your possession.", whispered);
        }

        public void UseItem(User user, Player player, Item item, params string[] arguments) {
            if(player.CurrentHP == 0) {
                context.GetModule<StreamModule>().SendMessage(user.Service, user.Name, "I am terribly sorry to inform you that you died quite a while ago.", false);
                return;
            }

            if (!RemoveItem(player.UserID, item.ID, 1))
            {
                context.GetModule<StreamModule>().SendMessage(user.Service, user.Name, $"And you are absolutely sure {item.GetEnumerationName()} is in your possession?.", false);
                return;
            }

            if (!string.IsNullOrEmpty(item.Command)) {
                string[] commandarguments = item.Command.Split(' ').Concat(arguments).ToArray();
                ICommandModule module = context.GetModuleByKey<ICommandModule>(commandarguments[0]);
                if (module == null)
                {
                    Logger.Error(this, $"Invalid command in item {item.Name}.", $"Module '{commandarguments[0]}' not found.");
                }
                else
                {
                    try
                    {
                        module.ProcessCommand(commandarguments[1], new[] { user.Service, user.Name}.Concat(commandarguments.Skip(2)).ToArray());
                    }
                    catch (Exception e)
                    {
                        Logger.Error(this, $"Error executing item command of item '{item.Name}'.", e);
                        return;
                    }
                }
            }

            if(!string.IsNullOrEmpty(item.CommandMessage))
                new ItemUseContext(context.GetModule<RPGMessageModule>().Create(), user, item).Send(item.CommandMessage);

            if (item.Type != ItemType.Consumable && item.Type != ItemType.Potion)
                return;

            RPGMessageBuilder message = context.GetModule<RPGMessageModule>().Create();

            bool critical = RNG.XORShift64.NextFloat() < (float)player.Luck / Math.Max(1, item.UsageOptimum);
            int hp = Math.Min(item.HP * (critical ? 2 : 1), player.MaximumHP - player.CurrentHP);
            int mp = Math.Min(item.MP * (critical ? 2 : 1), player.MaximumMP - player.CurrentMP);

            message.User(user).Text(item.IsFluid ? " drinks " : " eats ").ItemUsage(item, critical);
            if (hp > 0 || mp > 0)
            {
                player.CurrentHP = Math.Min(player.CurrentHP + hp, player.MaximumHP);
                player.CurrentMP = Math.Min(player.CurrentMP + mp, player.MaximumMP);
                context.GetModule<PlayerModule>().UpdateRunningStats(player.UserID, player.CurrentHP, player.CurrentMP);

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
                message.Text(" for no reason.");
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
                    Item pooitem = context.GetModule<ItemModule>().GetItem("Poo");
                    message.Text(" a pile of ").Item(pooitem);
                    context.GetModule<InventoryModule>().AddItem(player.UserID, pooitem.ID, 1, true);
                }

                if (player.Pee >= 100)
                {
                    if(poo) {
                        if(vomit)
                            message.Text(", ");
                        else message.Text(" and ");
                    }

                    player.Pee -= 100;
                    Item peeitem = context.GetModule<ItemModule>().GetItem("Pee");
                    message.Text(" a puddle of ").Item(peeitem);
                    context.GetModule<InventoryModule>().AddItem(player.UserID, peeitem.ID, 1, true);
                }

                if(player.Vomit >= 100) {
                    player.Vomit -= 100;
                    if(pee || poo)
                        message.Text(" and ");

                    Item vomititem = context.GetModule<ItemModule>().GetItem("Vomit");
                    message.Text(" an accumulation of finest ").Item(vomititem);
                    context.GetModule<InventoryModule>().AddItem(player.UserID, vomititem.ID, 1, true);
                }

                message.Text(".");
            }
            message.Send();
            context.GetModule<PlayerModule>().UpdatePeeAndPoo(player.UserID, player.Pee, player.Poo, player.Vomit);
        }

        void UseItem(string service, string username, string[] arguments, bool whispered) {
            if (arguments.Length == 0) {
                context.GetModule<StreamModule>().SendMessage(service, username, "Well, you have to specify the name of the item to use", whispered);
                return;
            }

            int index = 0;
            Item item = context.GetModule<ItemModule>().RecognizeItem(arguments, ref index);
            if(item == null) {
                context.GetModule<StreamModule>().SendMessage(service, username, $"An item of the name '{string.Join(" ", arguments)}' is unknown.", whispered);
                return;
            }

            if(item.Type == ItemType.Armor || item.Type == ItemType.Weapon) {
                context.GetModule<EquipmentModule>().Equip(service, username, context.GetModule<PlayerModule>().GetPlayer(service, username), item);
                return;
            }

            if(item.Type != ItemType.Consumable && item.Type != ItemType.Potion && string.IsNullOrEmpty(item.Command)) {
                context.GetModule<StreamModule>().SendMessage(service, username, $"Yeah sure ... use {item.Name} ... get real!", whispered);
                return;
            }

            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            Player player = context.GetModule<PlayerModule>().GetPlayer(user.ID);
            if (player == null)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, "Umm ... you do not seem to be a player in this channel.", whispered);
                return;
            }

            context.GetModule<SkillModule>().ModifyPlayerStats(player);
            context.GetModule<EffectModule>().ModifyPlayerStats(player);

            UseItem(user, player, item, arguments.Skip(index).ToArray());
        }

        void ShowInventory(string service, string user, bool whispered) {
            Player player = context.GetModule<PlayerModule>().GetPlayer(service, user);
            context.GetModule<StreamModule>().SendMessage(service, user, string.Join(", ", context.Database.LoadEntities<FullInventoryItem>().Where(p=>p.PlayerID==player.UserID).Execute().Select(i=>$"{i.Quantity} x {i.Name}")), whispered);
        }

        string IStreamCommandHandler.ProvideHelp(string command) {
            switch(command) {
                case "inventory":
                    return "Displays all items currently in your inventory.";
                case "use":
                    return "Uses an item in your inventory";
                case "drop":
                    return "Drops an item from your inventory";
                case "craft":
                    return "Tries to craft a new item using items from your inventory";
                case "give":
                    return "Gives an item to another player";
                default:
                    throw new StreamCommandException($"Command {command} not implemented by this module.");
            }
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "add":
                    AddItem(context.GetModule<PlayerModule>().GetExistingPlayer(arguments[0], arguments[1]).UserID, context.GetModule<ItemModule>().GetItemID(arguments.Skip(2).ToArray()), 1, true);
                    break;
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module.");
            }
        }
    }
}