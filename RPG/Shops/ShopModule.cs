using System;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Core.Timer;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Data;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Shops {

    /// <summary>
    /// module managing shop functions
    /// </summary>
    [Dependency(nameof(InventoryModule), DependencyType.Type)]
    [Dependency(nameof(PlayerModule), DependencyType.Type)]
    [Dependency(nameof(SkillModule), DependencyType.Type)]
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(ItemModule), DependencyType.Type)]
    [Dependency(nameof(MessageModule), DependencyType.Type)]
    [Dependency(nameof(TimerModule), DependencyType.Type)]
    [Dependency(nameof(GameMessageModule), DependencyType.Type)]
    public class ShopModule : IInitializableModule, IStreamCommandHandler, ITimerService {
        readonly Context context;
        readonly ShopEventType[] eventtypes = (ShopEventType[])Enum.GetValues(typeof(ShopEventType));
        readonly ShopQuirkType[] quirktypes = (ShopQuirkType[])Enum.GetValues(typeof(ShopQuirkType));

        TravelingMerchant travelingmerchant;
        double mood;

        readonly string[] insultkeywords = {
            "ass", "fuck", "bitch", "cunt", "whore", "penis", "arse", "butt", "retard", "bum", "shit", "idiot", "dildo", "dick", "cock", "suck"
        };

        /// <summary>
        /// creates a new <see cref="ShopModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public ShopModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// current mood of the shopkeeper
        /// </summary>
        public double Mood
        {
            get { return mood; }
            set
            {
                mood = Math.Min(1.0, Math.Max(0.0, value));
                context.Settings.Set(this, "mood", value);
            }
        }

        public bool IsInsultNecessaryToSell(long playerid, long itemid) {
            return Mood < 0.5 || context.Database.Load<ShopQuirk>(DBFunction.Count).Where(q => (q.Type == ShopQuirkType.Grudge && q.ID == playerid) || (q.Type == ShopQuirkType.Phobia && q.ID == itemid)).ExecuteScalar<int>() > 0;
        }

        void IStreamCommandHandler.ProcessStreamCommand(StreamCommand command) {
            switch(command.Command) {
                case "advise":
                    Advise(command.Service, command.User, command.Arguments, command.IsWhispered);
                    break;
                case "buy":
                    Buy(command.Service, command.User, command.Arguments, command.IsWhispered);
                    break;
                case "sell":
                    Sell(command.Service, command.User, command.Arguments, command.IsWhispered);
                    break;
                case "stock":
                    Stock(command.Service, command.User, command.Arguments, command.IsWhispered);
                    break;
                default:
                    throw new StreamCommandException($"Command '{command.Command}' not implemented by this module");
            }
        }

        void Advise(string service, string user, string[] arguments, bool isWhispered) {
            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, user);

            AdvisedItem[] adviseditems = context.Database.LoadEntities<AdvisedItem>().Where(i => i.PlayerID == player.UserID).Execute().OrderByDescending(i=>i.Value).ToArray();
            if(adviseditems.Length == 0) {
                context.GetModule<StreamModule>().SendMessage(service, user, "The shopkeeper is sad to inform you, that he can't do absolutely nothing to improve your ugly appearance.");
            }
            else {
                AdvisedItem item = adviseditems.First();

                int hagglelevel = context.GetModule<SkillModule>().GetSkillLevel(context.GetModule<PlayerModule>().GetExistingPlayer(service, user).UserID, SkillType.Haggler);
                double discount = GetDiscount(hagglelevel);

                int price = (int)(item.Value * (1.0 + GetQuantityFactor(item.Quantity) * 2 - discount) * item.Discount);
                context.GetModule<StreamModule>().SendMessage(service, user, $"The shopkeeper shows you {(item.Countable?"a ":"")}fine looking {item.Name} for a laughable price of {price} gold.", isWhispered);
            }
        }

        int GetSpecialPriceToBuy(int value, double discount) {
            return (int)(value * (2.2 - discount));
        }

        double GetDiscount(int hagglelevel) {
            switch(hagglelevel) {
                case 1:
                    return 0.12;
                case 2:
                    return 0.22;
                case 3:
                    return 0.3;
                default:
                    return 0.0;
            }
        }

        void Stock(string service, string user, string[] arguments, bool whispered) {
            if (arguments.Length == 0)
            {
                context.GetModule<StreamModule>().SendMessage(service, user, "You have to specify the name of the item to check.", whispered);
                return;
            }

            Item item = context.GetModule<ItemModule>().GetItem(arguments);
            if (item == null)
            {
                context.GetModule<StreamModule>().SendMessage(service, user, $"An item with the name '{string.Join(" ", arguments)}' is unknown.", whispered);
                return;
            }

            int hagglelevel = context.GetModule<SkillModule>().GetSkillLevel(context.GetModule<PlayerModule>().GetExistingPlayer(service, user).UserID, SkillType.Haggler);
            double discount = GetDiscount(hagglelevel);

            if(item.Type == ItemType.Special) {
                int price = GetSpecialPriceToBuy(item.Value, discount);
                context.GetModule<StreamModule>().SendMessage(service, user, $"{item.GetMultiple()} are available for a price of {price} per unit.", whispered);
            }
            else {
                ShopItem shopitem = context.Database.LoadEntities<ShopItem>().Where(i => i.ItemID == item.ID).Execute().FirstOrDefault();
                if(shopitem == null || shopitem.Quantity == 0) {
                    if(travelingmerchant != null && travelingmerchant.Type == item.Type) {
                        context.GetModule<StreamModule>().SendMessage(service, user, $"The traveling merchant would sell {item.Name} to you for {(int)(item.Value * (travelingmerchant.Price - discount))} Gold.");
                        return;
                    }
                    context.GetModule<StreamModule>().SendMessage(service, user, $"No known merchant has any {item.Name} currently.", whispered);
                    return;
                }

                int price = (int)(item.Value * (1.0 + GetQuantityFactor(shopitem.Quantity) * 2 - discount) * shopitem.Discount);
                context.GetModule<StreamModule>().SendMessage(service, user, $"{item.GetCountName(shopitem.Quantity)} {(shopitem.Quantity == 1 ? "is" : "are")} currently available for a price of {price} per unit.", whispered);
            }
        }

        void Sell(string service, string username, string[] arguments, bool whispered) {
            int argumentindex=0;
            int quantity = arguments.RecognizeQuantity(ref argumentindex);

            if (arguments.Length <= argumentindex)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, "You have to tell me what item to sell.", whispered);
                return;
            }

            Item item = context.GetModule<ItemModule>().RecognizeItem(arguments, ref argumentindex);
            if (item == null)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, "I don't understand what exactly you want to buy.", whispered);
                return;
            }

            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            Player player = context.GetModule<PlayerModule>().GetPlayer(service, username);
            InventoryItem inventoryitem = context.GetModule<InventoryModule>().GetItem(player.UserID, item.ID);
            if(inventoryitem == null) {
                context.GetModule<StreamModule>().SendMessage(service, username, $"With pride you present your {item.GetMultiple()} to the shopkeeper until you realize that your hands are empty.", whispered);
                return;
            }

            if(quantity == -1)
                quantity = inventoryitem.Quantity;

            if(quantity <= 0) {
                context.GetModule<StreamModule>().SendMessage(service, username, $"It wouldn't make sense to sell {item.GetCountName(quantity)}.", whispered);
                return;
            }

            if (item.Type == ItemType.Special)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, "You can not sell special items.", whispered);
                return;
            }

            bool insulted = DetermineInsult(arguments, argumentindex);
            if(!insulted) {
                if(Mood < 0.5) {
                    context.GetModule<StreamModule>().SendMessage(service, username, "The shopkeeper is not in the mood to do business.");
                    return;
                }

                if(context.Database.Load<ShopQuirk>(DBFunction.Count).Where(q => q.Type == ShopQuirkType.Phobia && q.ID == item.ID).ExecuteScalar<int>() > 0) {
                    context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" sees ").User(user).Text("'s ").Item(item, quantity).Text(", suddenly starts to scream and kicks ").User(user).Text(" out of the shop.").Send();
                    return;
                }

                if(context.Database.Load<ShopQuirk>(DBFunction.Count).Where(q => q.Type == ShopQuirkType.Grudge && q.ID == player.UserID).ExecuteScalar<int>() > 0) {
                    context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" casually ignores ").User(user).Text(".").Send();
                    return;
                }
            }

            SellItem(player.UserID, item, quantity, insulted ? 0.2 : 0.0);
        }

        public void SellItem(long playerid, long itemid, int quantity, double specialdiscount = 0.0)
        {
            SellItem(playerid, context.GetModule<UserModule>().GetUser(playerid), context.GetModule<ItemModule>().GetItem(itemid), quantity, specialdiscount);
        }

        public void SellItem(long playerid, Item item, int quantity, double specialdiscount = 0.0) {
            SellItem(playerid, context.GetModule<UserModule>().GetUser(playerid), item, quantity, specialdiscount);
        }

        public void SellItem(long playerid, User user, Item item, int quantity, double specialdiscount=0.0) {
            double discount = GetDiscount(context.GetModule<SkillModule>().GetSkillLevel(playerid, SkillType.Haggler));
            int shopquantity = context.Database.Load<ShopItem>(i => i.Quantity).Where(i => i.ItemID == item.ID).ExecuteScalar<int>();
            double shopdiscount = context.Database.Load<ShopItem>(i => i.Discount).Where(i => i.ItemID == item.ID).ExecuteScalar<double>();
            if (shopdiscount < 0.1)
                shopdiscount = 1.0;

            int price = (int)Math.Max(1, item.Value * quantity * (0.5 + GetQuantityFactor(shopquantity) + discount + specialdiscount) * shopdiscount);
            if (price == 0) price = 1;

            context.GetModule<InventoryModule>().RemoveItem(playerid, item.ID, quantity);
            context.Database.Update<Player>().Set(p => p.Gold == p.Gold + price).Where(p => p.UserID == playerid).Execute();

            AddItem(item.ID, quantity);

            Mood += 0.15 * (1.0 - discount);

            context.GetModule<RPGMessageModule>().Create().User(user).Text(" sold ").Item(item, quantity).Text(" to ").ShopKeeper().Text(" for ").Gold(price).Text(".").Send();
        }

        /// <summary>
        /// adds an item to the shop
        /// </summary>
        /// <param name="itemid">id of item to add</param>
        /// <param name="quantity">quantity to add to the shop</param>
        public void AddItem(long itemid, int quantity) {
            if(context.Database.Update<ShopItem>().Set(i => i.Quantity == i.Quantity + quantity).Where(i => i.ItemID == itemid).Execute() == 0)
                context.Database.Insert<ShopItem>().Columns(i => i.ItemID, i => i.Quantity, i => i.Discount).Values(itemid, quantity, 1.0).Execute();
        }

        double GetQuantityFactor(int amount) {
            return Math.Max(0.0, 1.0 - amount / 1000.0);
        }

        bool DetermineInsult(string[] arguments, int argumentindex) {
            for(int i = argumentindex; i < arguments.Length; ++i) {
                string caseless = arguments[i].ToLower();
                if(insultkeywords.Any(w => caseless.Contains(w)))
                    return true;
            }
            return false;
        }

        void Buy(string service, string username, string[] arguments, bool whispered) {
            int argumentindex=0;
            int quantity = arguments.RecognizeQuantity(ref argumentindex);

            if(arguments.Length <= argumentindex) {
                context.GetModule<StreamModule>().SendMessage(service, username, "You have to specify the name of the item to buy.", whispered);
                return;
            }

            Item item = context.GetModule<ItemModule>().RecognizeItem(arguments, ref argumentindex);
            if(item == null) {
                context.GetModule<StreamModule>().SendMessage(service, username, "I don't understand what exactly you want to buy.", whispered);
                return;
            }

            if(quantity == -1)
                quantity = 1;

            if(quantity <= 0) {
                context.GetModule<StreamModule>().SendMessage(service, username, $"It wouldn't make sense to buy {item.GetCountName(quantity)}.", whispered);
                return;
            }

            bool insulted = DetermineInsult(arguments, argumentindex);

            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            Player player = context.GetModule<PlayerModule>().GetPlayer(service, username);

            double discount = GetDiscount(context.GetModule<SkillModule>().GetSkillLevel(player.UserID, SkillType.Haggler));
            if(insulted)
                discount -= 0.2;

            int price;
            if(item.Type == ItemType.Special) {
                price = GetSpecialPriceToBuy(item.Value, discount);
            }
            else { 
                ShopItem shopitem = context.Database.LoadEntities<ShopItem>().Where(i => i.ItemID == item.ID).Execute().FirstOrDefault();
                if(shopitem == null || shopitem.Quantity < quantity) {
                    if(travelingmerchant != null && travelingmerchant.Type == item.Type) {
                        if (item.LevelRequirement > player.Level && !insulted)
                        {
                            context.GetModule<StreamModule>().SendMessage(service, username, $"The merchant denies you access to the item as you wouldn't be able to use it. (Level {item.LevelRequirement})");
                            return;
                        }

                        BuyFromTravelingMerchant(service, username, player, item, quantity, discount, whispered);
                        return;
                    }

                    if(shopitem == null || shopitem.Quantity == 0)
                        context.GetModule<StreamModule>().SendMessage(service, username, "The shopkeeper tells you that he has nothing on stock.", whispered);
                    else context.GetModule<StreamModule>().SendMessage(service, username, $"The shopkeeper tells you that he only has {item.GetCountName(shopitem.Quantity)}.", whispered);
                    return;
                }

                if(!insulted) {
                    if(Mood < 0.5) {
                        context.GetModule<StreamModule>().SendMessage(service, username, "The shopkeeper does not see a point in doing anything anymore.");
                        return;
                    }

                    if(context.Database.Load<ShopQuirk>(DBFunction.Count).Where(q => q.Type == ShopQuirkType.Phobia && q.ID == item.ID).ExecuteScalar<int>() > 0) {
                        context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" tells ").User(user).Text(" that no one should mess with ").ItemMultiple(item).Text(" since it is devil's work.").Send();
                        return;
                    }

                    if(context.Database.Load<ShopQuirk>(DBFunction.Count).Where(q => q.Type == ShopQuirkType.Nerd && q.ID == item.ID).ExecuteScalar<int>() > 0) {
                        context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" does not want to share his beloved ").ItemMultiple(item).Text(".").Send();
                        return;
                    }

                    if(context.Database.Load<ShopQuirk>(DBFunction.Count).Where(q => q.Type == ShopQuirkType.Grudge && q.ID == player.UserID).ExecuteScalar<int>() > 0) {
                        context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" casually ignores ").User(user).Text(".").Send();
                        return;
                    }
                }

                if (item.LevelRequirement > player.Level && !insulted) {
                    context.GetModule<StreamModule>().SendMessage(service, username, $"The shopkeeper denies you access to the item as you wouldn't be able to use it. (Level {item.LevelRequirement})");
                    return;
                }

                price = (int)(item.Value * quantity * (1.0 + GetQuantityFactor(shopitem.Quantity) * 2 - discount) * shopitem.Discount);
            }

            if (price > player.Gold) {
                context.GetModule<StreamModule>().SendMessage(service, username, $"Sadly you don't have enough gold. You would need {price} gold to buy {item.GetCountName(quantity)}", whispered);
                return;
            }

            AddInventoryItemResult result = context.GetModule<InventoryModule>().AddItem(player.UserID, item.ID, quantity);
            switch(result) {
                case AddInventoryItemResult.Success:
                case AddInventoryItemResult.SuccessFull:
                    Aggregate agg = Aggregate.Max(Constant.Create(0), EntityField.Create<Player>(pl => pl.Gold));
                    context.Database.Update<Player>().Set(p => p.Gold == agg.Int - price).Where(p => p.UserID == player.UserID).Execute();

                    if (item.Type!=ItemType.Special)
                        context.Database.Update<ShopItem>().Set(i => i.Quantity == i.Quantity - quantity).Where(i => i.ItemID == item.ID).Execute();

                    RPGMessageBuilder message = context.GetModule<RPGMessageModule>().Create();
                    message.User(user).Text(" has bought ").Item(item, quantity).Text(" from ").ShopKeeper().Text(" for ").Gold(price).Text(".");

                    if(result == AddInventoryItemResult.SuccessFull)
                        message.User(user).Text(" is now encumbered.");

                    message.Send();
                    Mood += 0.05 * (1.0 - discount);
                    break;
                case AddInventoryItemResult.InventoryFull:
                    context.GetModule<RPGMessageModule>().Create().User(user).Text(" carries to much to buy ").Item(item, quantity).Text(".").Send();
                    break;
            }
        }

        void BuyFromTravelingMerchant(string service, string username, Player player, Item item, int quantity, double discount, bool whispered) {
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);

            int price = (int)(item.Value * (travelingmerchant.Price - discount));
            if (price > player.Gold)
            {
                context.GetModule<StreamModule>().SendMessage(service, username, $"Sadly you don't have enough gold. You would need {price} gold to buy {item.GetCountName(quantity)}", whispered);
                return;
            }

            AddInventoryItemResult result = context.GetModule<InventoryModule>().AddItem(player.UserID, item.ID, quantity);
            switch (result)
            {
                case AddInventoryItemResult.Success:
                case AddInventoryItemResult.SuccessFull:
                    Aggregate agg = Aggregate.Max(Constant.Create(0), EntityField.Create<Player>(pl => pl.Gold));
                    context.Database.Update<Player>().Set(p => p.Gold == agg.Int-price).Where(p=>p.UserID==player.UserID).Execute();


                    RPGMessageBuilder message = context.GetModule<RPGMessageModule>().Create().User(user).Text(" bought ").Item(item, quantity).Text(" and spent ").Gold(price);

                    if(result == AddInventoryItemResult.SuccessFull)
                        message.Text(" and is now encumbered.");
                    else message.Text(".");

                    message.Send();
                    break;
                case AddInventoryItemResult.InventoryFull:
                    context.GetModule<StreamModule>().SendMessage(service, username, $"You don't have room in your inventory to buy {quantity} {item.Name}", whispered);
                    break;
            }
        }

        string IStreamCommandHandler.ProvideHelp(string command) {
            switch(command) {
                case "advise":
                    return "Gets an advise which equipment the shopkeeper has on stock which would improve your stats.";
                case "buy":
                    return "Buys an item from the shop. Syntax: !buy <item> [amount]. If no amount is specified 1 is considered to be the quantity to buy.";
                case "sell":
                    return "Sells an item to the shop. Syntax: !sell <item> [amount]. If no amount is specified, all items will be sold.";
                case "stock":
                    return "Determines how many units of an item are available in shop. Syntax: !stock <item>.";
                default:
                    throw new StreamCommandException($"Command {command} not implemented by this module");
            }
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<ShopItem>();
            context.Database.UpdateSchema<ShopQuirk>();
            context.Database.UpdateSchema<FullShopItem>();
            context.Database.UpdateSchema<AdvisedItem>();
            mood = context.Settings.Get(this, "mood", 1.0);
            context.GetModule<StreamModule>().RegisterCommandHandler(this, "buy", "sell", "stock", "advise");
            context.GetModule<TimerModule>().AddService(this, 300.0);
        }

        double GetEventChance(ShopEventType type) {
            switch(type) {
                case ShopEventType.Discount:
                    return 0.6;
                case ShopEventType.Quirk:
                    return 0.25;
                case ShopEventType.Bored:
                    return 1.0 - mood;
                /*case ShopEventType.Craft:
                    return 0.1;*/
                default:
                    return 0.0;
            }
        }

        void ITimerService.Process(double time) {
            if(travelingmerchant != null) {
                travelingmerchant = null;
                context.GetModule<StreamModule>().BroadcastMessage("The traveling merchant is leaving town.");
                context.GetModule<MessageModule>().AddMessage(Message.Parse("The traveling merchant is leaving town."));
            }
            else if(RNG.XORShift64.NextFloat() < 0.13) {
                ItemType type = new[] { ItemType.Armor, ItemType.Weapon, ItemType.Book, ItemType.Potion }.RandomItem(RNG.XORShift64);
                travelingmerchant = new TravelingMerchant
                {
                    Type = type,
                    Price = 4.5 + RNG.XORShift64.NextDouble()
                };
                context.GetModule<StreamModule>().BroadcastMessage($"A traveling merchant trading with {type.ToString().GetMultiple()} has appeared in town.");
                context.GetModule<MessageModule>().AddMessage(Message.Parse($"A traveling merchant trading with [c=$0]{type.ToString().GetMultiple()}[/] has appeared in town.", AdventureColors.Item));
            }

            Item item;
            long id;
            switch(eventtypes.RandomItem(GetEventChance, RNG.XORShift64)) {
                case ShopEventType.Discount:
                    long discountitemid = context.Database.Load<ShopItem>(i => i.ItemID).Where(i => i.Quantity > 0).OrderBy(DBFunction.Random).Limit(1).ExecuteScalar<long>();
                    item = context.GetModule<ItemModule>().GetItem(discountitemid);
                    if(item != null) {
                        double discount = 0.75 + RNG.XORShift64.NextDouble() * 0.5;
                        context.Database.Update<ShopItem>().Set(i => i.Discount == discount).Where(i => i.ItemID == discountitemid).Execute();
                        if(discount < 0.8)
                            context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" wants to get rid of ").ItemMultiple(item).Text(".").Send();
                        else if(discount < 0.95)
                            context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" wants to push the trade of ").ItemMultiple(item).Text(" a bit.").Send();
                        else if(discount > 1.05)
                            context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" is interested in buying ").ItemMultiple(item).Text(".").Send();
                        else if(discount > 1.2)
                            context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" really wants to top up his stock of ").ItemMultiple(item).Text(".").Send();
                        else
                            context.GetModule<RPGMessageModule>().Create().Text("The prices of ").ItemMultiple(item).Text(" have stabilised.").Send();
                    }
                    break;
                case ShopEventType.Quirk:
                    if(context.Database.Load<ShopQuirk>(DBFunction.Count).ExecuteScalar<int>() >= 5 || RNG.XORShift64.NextFloat() < 0.35) {
                        ShopQuirk quirk = context.Database.LoadEntities<ShopQuirk>().OrderBy(new OrderByCriteria(DBFunction.Random)).Limit(1).Execute().FirstOrDefault();
                        if(quirk != null) {
                            context.Database.Delete<ShopQuirk>().Where(q => q.ID == quirk.ID && q.Type == quirk.Type).Execute();
                            switch(quirk.Type) {
                                case ShopQuirkType.Grudge:
                                    context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" thinks ").User(context.GetModule<UserModule>().GetUser(quirk.ID)).Text(" is actually not that bad of a person.").Send();
                                    break;
                                case ShopQuirkType.Nerd:
                                    context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" is over his love for ").ItemMultiple(context.GetModule<ItemModule>().GetItem(quirk.ID)).Text(".").Send();
                                    break;
                                case ShopQuirkType.Phobia:
                                    context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" isn't scared of ").ItemMultiple(context.GetModule<ItemModule>().GetItem(quirk.ID)).Text(" anymore.").Send();
                                    break;
                            }
                        }
                    }
                    else {
                        ShopQuirkType quirk = quirktypes.RandomItem(RNG.XORShift64);
                        id = 0;

                        switch(quirk) {
                            case ShopQuirkType.Grudge:
                                id = context.GetModule<AdventureModule>().Adventures.Select(a => a.Player).RandomItem(RNG.XORShift64);
                                break;
                            case ShopQuirkType.Nerd:
                            case ShopQuirkType.Phobia:
                                id = context.Database.Load<ShopItem>(i => i.ItemID).OrderBy(DBFunction.Random).Limit(1).ExecuteScalar<long>();
                                break;
                        }

                        if(id > 0) {
                            if(context.Database.Load<ShopQuirk>(DBFunction.Count).Where(q => q.ID == id && q.Type == quirk).ExecuteScalar<int>() == 0) {
                                context.Database.Insert<ShopQuirk>().Columns(q => q.Type, q => q.ID).Values(quirk, id).Execute();
                                switch(quirk) {
                                    case ShopQuirkType.Grudge:
                                        context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" starts to dislike ").User(context.GetModule<UserModule>().GetUser(id)).Text(".").Send();
                                        break;
                                    case ShopQuirkType.Nerd:
                                        context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" wants to have all ").ItemMultiple(context.GetModule<ItemModule>().GetItem(id)).Text(" for himself.").Send();
                                        break;
                                    case ShopQuirkType.Phobia:
                                        context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" thinks ").ItemMultiple(context.GetModule<ItemModule>().GetItem(id)).Text(" are haunting him.").Send();
                                        break;
                                }
                            }
                        }
                    }
                    break;
                case ShopEventType.Bored:
                    id = context.GetModule<AdventureModule>().Adventures.Select(a => a.Player).RandomItem(RNG.XORShift64);
                    if(id > 0) {
                        switch(RNG.XORShift64.NextInt(3)) {
                            case 0:
                                item = context.GetModule<ItemModule>().GetItem("Poo");
                                context.GetModule<InventoryModule>().AddItem(id, item.ID, 1, true);
                                context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" sneaks up and throws a pile of ").Item(item).Text(" at ").User(id).Text(".").Send();
                                break;
                            case 1:
                                item = context.GetModule<ItemModule>().GetItem("Pee");
                                context.GetModule<InventoryModule>().AddItem(id, item.ID, 1, true);
                                context.GetModule<RPGMessageModule>().Create().User(id).Text(" turns around realizing a hit of a ray of ").Item(item).Text(" created by ").ShopKeeper().Text(".").Send();
                                break;
                            case 2:
                                item = context.GetModule<ItemModule>().GetItem("Vomit");
                                context.GetModule<InventoryModule>().AddItem(id, item.ID, 1, true);
                                context.GetModule<RPGMessageModule>().Create().User(id).Text(" is feeling their head getting moisture since it is covered by ").Item(item).Text(" left by ").ShopKeeper().Text(".").Send();
                                break;
                        }
                    }
                    break;
                case ShopEventType.Craft:
                    break;
            }
            Mood -= 0.03 * time / 300.0;
        }
    }
}