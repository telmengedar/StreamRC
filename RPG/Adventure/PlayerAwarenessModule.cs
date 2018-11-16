using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.RPG.Equipment;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Items.Recipes;
using StreamRC.RPG.Players;
using StreamRC.RPG.Players.Skills;
using StreamRC.RPG.Shops;

namespace StreamRC.RPG.Adventure {

    [Module]
    public class PlayerAwarenessModule {
        readonly InventoryModule inventory;
        readonly PlayerModule players;
        readonly SkillModule skills;
        readonly ItemModule items;
        readonly ShopModule shop;
        readonly EquipmentModule equipmentmodule;
        readonly AdventureModule adventure;
        readonly ConvenienceModule convenience;

        readonly TimeSpan threshold = TimeSpan.FromSeconds(1.0);
        readonly Dictionary<long, AwarenessContext> afkdetection = new Dictionary<long, AwarenessContext>();
         
        public PlayerAwarenessModule(AdventureModule adventuremodule, InventoryModule inventory, PlayerModule players, SkillModule skills, ItemModule items, ShopModule shop, EquipmentModule equipmentmodule, AdventureModule adventure, ConvenienceModule convenience) {
            this.inventory = inventory;
            this.players = players;
            this.skills = skills;
            this.items = items;
            this.shop = shop;
            this.equipmentmodule = equipmentmodule;
            this.adventure = adventure;
            this.convenience = convenience;
            adventuremodule.PlayerActiveChanged += OnPlayerActiveChanged;
            adventuremodule.PlayerActiveTrigger += OnPlayerActive;
            adventuremodule.ItemFound += OnItemFound;
            inventory.PlayerEncumbered += OnPlayerEncumbered;
            players.HealthChanged += OnHealthChanged;
            skills.SkillChanged += OnSkillChanged;
        }

        void OnItemFound(long playerid, long itemid, int quantity) {
            AwarenessContext playercontext;
            if (!afkdetection.TryGetValue(playerid, out playercontext))
                return;

            // automatic item selling is done by level 1
            if(playercontext.Level < 1)
                return;

            Item item = items.GetItem(itemid);
            if(item.Type == ItemType.Gold)
                return;

            if(items.IsIngredient(itemid)) {

                // automatic crafting is done by level 3
                if(playercontext.Level >= 3) {
                    ItemRecipe[] recipes = items.GetRecipes(itemid).OrderByDescending(r => r.Ingredients.Length).ToArray();
                    foreach(ItemRecipe recipe in recipes) {
                        if(inventory.HasItems(playerid, recipe.Ingredients.Select(i => i.Item).ToArray())) {
                            inventory.CraftItem(playerid, items.GetItems(recipe.Ingredients.Select(i => i.Item)).ToArray());
                            return;
                        }
                    }
                }
            }

            if(item.Type == ItemType.Consumable) {
                Player player = players.GetExistingPlayer(playerid);
                if(item.HP < player.MaximumHP * 0.35) {
                    shop.SellItem(playerid, item, quantity, shop.IsInsultNecessaryToSell(playerid, itemid) ? 0.2 : 0.0);
                    return;
                }
            }
            else if (item.Type == ItemType.Weapon || item.Type == ItemType.Armor) {
                if (item.LevelRequirement > players.GetLevel(playerid))
                    return;

                EquipmentBonus bonus = equipmentmodule.GetEquipmentBonus(playerid, item.GetTargetSlot());
                if((item.Type == ItemType.Weapon && bonus.Damage >= item.Damage) || (item.Type == ItemType.Armor && bonus.Armor >= item.Armor))
                    shop.SellItem(playerid, item, quantity, shop.IsInsultNecessaryToSell(playerid, itemid) ? 0.2 : 0.0);
                else {
                    EquipmentItem olditem = equipmentmodule.Equip(playerid, item, item.GetTargetSlot());
                    if(quantity > 1)
                        shop.SellItem(playerid, item, quantity - 1, shop.IsInsultNecessaryToSell(playerid, itemid) ? 0.2 : 0.0);
                    shop.SellItem(playerid, olditem.ItemID, 1, shop.IsInsultNecessaryToSell(playerid, olditem.ItemID) ? 0.2 : 0.0);
                }
                return;
            }

            shop.SellItem(playerid, item, quantity, shop.IsInsultNecessaryToSell(playerid, itemid) ? 0.2 : 0.0);

            if(playercontext.Level >= 3) {
                int sizethreshold = (int)(inventory.GetMaximumInventorySize(playerid) * 0.8);

                if(inventory.GetInventorySize(playerid) > sizethreshold) {
                    List<FullInventoryItem> inventorylist = new List<FullInventoryItem>(inventory.GetInventoryItems(playerid));

                    while(inventorylist.Count > sizethreshold) {
                        FullInventoryItem sellitem = inventorylist.Where(i => i.Type == ItemType.Misc && i.Name != "Pee" && i.Name != "Poo").OrderBy(i => i.Value).FirstOrDefault();
                        if(sellitem == null)
                            sellitem = inventorylist.FirstOrDefault(i => i.Type == ItemType.Potion && i.HP == 0);
                        if(sellitem == null)
                            sellitem = inventorylist.Where(i => i.Type == ItemType.Consumable).OrderBy(i => i.Value).FirstOrDefault();
                        if(sellitem == null)
                            break;

                        shop.SellItem(playerid, sellitem.ID, sellitem.Quantity, shop.IsInsultNecessaryToSell(playerid, sellitem.ID) ? 0.2 : 0.0);
                        inventorylist.Remove(sellitem);
                    }
                }
                
            }
        }

        void OnSkillChanged(long playerid, SkillType skilltype, int level)
        {
            if(skilltype == SkillType.Awareness) {
                AwarenessContext playercontext;
                if (!afkdetection.TryGetValue(playerid, out playercontext))
                    return;

                playercontext.Level = level;
            }
        }

        void OnPlayerActive(long playerid)
        {
            AwarenessContext olayercontext;
            if (!afkdetection.TryGetValue(playerid, out olayercontext))
                return;

            if ((DateTime.Now - olayercontext.LastTrigger) < threshold)
                return;

            olayercontext.LastTrigger = DateTime.Now;
            olayercontext.AFKIndications = 0;
        }

        void IncreaseAFKIndicator(long playerid, AwarenessContext playercontext) {
            Logger.Info(this, $"AFK signs for player '{playerid}'");
            if (++playercontext.AFKIndications >= 5)
                adventure.Rest(playerid);
        }

        void OnPlayerEncumbered(long playerid) {
            AwarenessContext playercontext;
            if(!afkdetection.TryGetValue(playerid, out playercontext))
                return;

            if((DateTime.Now - playercontext.LastTrigger) < threshold)
                return;

            playercontext.LastTrigger = DateTime.Now;
            IncreaseAFKIndicator(playerid, playercontext);
        }

        void OnHealthChanged(long playerid, int hp, int maxhp, int hpchange)
        {
            AwarenessContext playercontext;
            if (!afkdetection.TryGetValue(playerid, out playercontext))
                return;

            if ((DateTime.Now - playercontext.LastTrigger) < threshold)
                return;

            playercontext.LastTrigger = DateTime.Now;

            if(hp > 0 && hp <= playercontext.MaxDamage) {
                if(playercontext.Level >= 2) {
                    if(skills.CanCastHeal(playerid) || inventory.HasHealingItems(playerid))
                        convenience.Heal(playerid);
                }
                else IncreaseAFKIndicator(playerid, playercontext);
            }

            if (-hpchange > playercontext.MaxDamage)
                playercontext.MaxDamage = -hpchange;
        }

        void OnPlayerActiveChanged(long playerid, bool isactive) {
            if(isactive)
                afkdetection[playerid] = new AwarenessContext {
                    AFKIndications = 0,
                    LastTrigger = DateTime.Now,
                    Level = skills.GetSkillLevel(playerid, SkillType.Awareness)
                };

            else afkdetection.Remove(playerid);
        }
    }
}