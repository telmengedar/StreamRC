using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Emotions;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.RPG.Players.Skills;
using StreamRC.RPG.Shops;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Adventure.Exploration {
    public class ExplorationLogic : IAdventureLogic {
        readonly Context context;
        readonly AdventureEvent[] eventtypes = (AdventureEvent[])Enum.GetValues(typeof(AdventureEvent));

        public ExplorationLogic(Context context) {
            this.context = context;
        }

        double GetEventTypeValue(AdventureEvent type, Player player)
        {
            switch (type)
            {
                default:
                case AdventureEvent.Nothing:
                    return 1.0;
                case AdventureEvent.Item:
                    return 0.22;
                case AdventureEvent.Chest:
                    return 0.07;
                case AdventureEvent.Monster:
                    return 0.09;
            }
        }

        void ExecuteChest(Player player) {
            double chance = 0.8;

            List<FoundItem> items=new List<FoundItem>();

            while(RNG.XORShift64.NextDouble() < chance) {
                Item item = context.GetModule<ItemModule>().SelectItem(player.Level, player.Luck * 2);
                if (item == null)
                    return;

                FoundItem found = items.FirstOrDefault(i => i.Item.ID == item.ID);
                if(found == null) {
                    found = new FoundItem {
                        Item = item
                    };
                    items.Add(found);
                }

                if(item.Type == ItemType.Gold)
                    found.Quantity += 1 + player.Luck * 30 + RNG.XORShift64.NextInt((int)Math.Max(1, player.Level * player.Luck * 0.75));
                else ++found.Quantity;

                chance *= 0.8;
            }

            User user = context.GetModule<UserModule>().GetUser(player.UserID);

            if (items.Count == 0) {
                context.GetModule<RPGMessageModule>().Create().User(user).Text(" has found a chest ... which was empty.").Emotion(EmotionType.FuckYou).Send();
                return;
            }

            List<FoundItem> dropped=new List<FoundItem>();
            AddInventoryItemResult allresult = AddInventoryItemResult.Success;
            for(int i=items.Count-1;i>=0;--i) {
                FoundItem item = items[i];

                if(item.Item.Type == ItemType.Gold) {
                    context.GetModule<PlayerModule>().UpdateGold(player.UserID, item.Quantity);
                    continue;
                }

                AddInventoryItemResult result = context.GetModule<InventoryModule>().AddItem(player.UserID, item.Item.ID, item.Quantity);
                if(result == AddInventoryItemResult.InvalidItem)
                    continue;

                if(result > allresult)
                    allresult = result;

                if(result == AddInventoryItemResult.InventoryFull) {
                    context.GetModule<ShopModule>().AddItem(item.Item.ID, item.Quantity);
                    dropped.Add(item);
                    items.RemoveAt(i);
                }
            }

            RPGMessageBuilder message = context.GetModule<RPGMessageModule>().Create();
            message.User(user).Text(" opened a chest ");


            if(items.Count > 0) {
                if(dropped.Count == 0 && allresult<=AddInventoryItemResult.Success)
                    message.Text(" and found");
                else message.Text(" found");

                for(int i = 0; i < items.Count; ++i) {
                    if(i == 0)
                        message.Text(" ");
                    else if(i == items.Count - 1)
                        message.Text(" and ");
                    else
                        message.Text(", ");

                    FoundItem item = items[i];

                    message.Item(item.Item, item.Quantity);
                }
            }

            if(dropped.Count > 0) {
                if(allresult>AddInventoryItemResult.Success)
                    message.Text(", dropped");
                else message.Text(" and dropped");

                for (int i = 0; i < dropped.Count; ++i)
                {
                    if (i == 0)
                        message.Text(" ");
                    else if (i == items.Count - 1)
                        message.Text(" and ");
                    else
                        message.Text(", ");

                    FoundItem item = dropped[i];

                    message.Item(item.Item, item.Quantity);
                }
            }
            
            if(allresult > AddInventoryItemResult.Success)
                message.Text(" and is now encumbered.");
            else message.Text(".");

            message.Send();

            foreach(FoundItem item in items)
                context.GetModule<AdventureModule>().TriggerItemFound(player.UserID, item.Item.ID, item.Quantity);
        }

        void ExecuteItem(Player player)
        {
            Item item = context.GetModule<ItemModule>().SelectItem(player.Level, player.Luck);
            if (item == null)
                return;

            User user = context.GetModule<UserModule>().GetUser(player.UserID);

            if (item.Type == ItemType.Gold) {
                int quantity = 1 + player.Luck * 10 + RNG.XORShift64.NextInt((int)Math.Max(1, player.Level * player.Luck * 0.35));
                context.Database.Update<Player>().Set(p => p.Gold == p.Gold + quantity).Where(p => p.UserID == player.UserID).Execute();
                context.GetModule<RPGMessageModule>().Create().User(user).Text(" has found ").Item(item, quantity).Text(".").Send();
            }
            else
            {
                switch (context.GetModule<InventoryModule>().AddItem(player.UserID, item.ID, 1))
                {
                    case AddInventoryItemResult.SuccessFull:
                        context.GetModule<RPGMessageModule>().Create().User(user).Text(" has found ").Item(item).Text(" and is now encumbered.").Send();
                        context.GetModule<AdventureModule>().TriggerItemFound(player.UserID, item.ID, 1);
                        break;
                    case AddInventoryItemResult.InventoryFull:
                        context.GetModule<ShopModule>().AddItem(item.ID, 1);
                        context.GetModule<RPGMessageModule>().Create().User(user).Text(" is encumbered and dropped ").Item(item).Text(".").Send();
                        break;
                    default:
                        context.GetModule<RPGMessageModule>().Create().User(user).Text(" has found ").Item(item).Text(".").Send();                        
                        context.GetModule<AdventureModule>().TriggerItemFound(player.UserID, item.ID, 1);
                        break;
                }
            }
        }

        public AdventureStatus ProcessPlayer(long playerid) {
            Player player = context.GetModule<PlayerModule>().GetPlayer(playerid);
            context.GetModule<SkillModule>().ModifyPlayerStats(player);
            AdventureEvent @event = eventtypes.RandomItem(a => GetEventTypeValue(a, player), RNG.XORShift64);
            switch (@event)
            {
                case AdventureEvent.Item:
                    ExecuteItem(player);
                    break;
                case AdventureEvent.Chest:
                    ExecuteChest(player);
                    break;
                case AdventureEvent.Monster:
                    return AdventureStatus.MonsterBattle;
            }

            return Status;
        }

        public AdventureStatus Status => AdventureStatus.Exploration;
    }
}