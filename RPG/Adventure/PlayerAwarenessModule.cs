using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Equipment;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Items.Recipes;
using StreamRC.RPG.Players;
using StreamRC.RPG.Players.Skills;
using StreamRC.RPG.Shops;

namespace StreamRC.RPG.Adventure {

    [Dependency(nameof(AdventureModule))]
    [Dependency(nameof(InventoryModule))]
    [Dependency(nameof(PlayerModule))]
    public class PlayerAwarenessModule : IRunnableModule {
        readonly Context context;

        readonly TimeSpan threshold = TimeSpan.FromSeconds(1.0);
        readonly Dictionary<long, AwarenessContext> afkdetection = new Dictionary<long, AwarenessContext>();
         
        public PlayerAwarenessModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            context.GetModule<AdventureModule>().PlayerActiveChanged += OnPlayerActiveChanged;
            context.GetModule<AdventureModule>().PlayerActiveTrigger += OnPlayerActive;
            context.GetModule<AdventureModule>().ItemFound += OnItemFound;
            context.GetModule<InventoryModule>().PlayerEncumbered += OnPlayerEncumbered;
            context.GetModule<PlayerModule>().HealthChanged += OnHealthChanged;
            context.GetModule<SkillModule>().SkillChanged += OnSkillChanged;
        }

        void IRunnableModule.Stop()
        {
            context.GetModule<AdventureModule>().PlayerActiveChanged -= OnPlayerActiveChanged;
            context.GetModule<AdventureModule>().PlayerActiveTrigger -= OnPlayerActive;
            context.GetModule<AdventureModule>().ItemFound -= OnItemFound;
            context.GetModule<InventoryModule>().PlayerEncumbered -= OnPlayerEncumbered;
            context.GetModule<PlayerModule>().HealthChanged -= OnHealthChanged;
            context.GetModule<SkillModule>().SkillChanged -= OnSkillChanged;
        }

        void OnItemFound(long playerid, long itemid, int quantity) {
            AwarenessContext playercontext;
            if (!afkdetection.TryGetValue(playerid, out playercontext))
                return;

            // automatic item selling is done by level 1
            if(playercontext.Level < 1)
                return;

            Item item = context.GetModule<ItemModule>().GetItem(itemid);
            if(item.Type == ItemType.Gold)
                return;

            if(context.GetModule<ItemModule>().IsIngredient(itemid)) {

                // automatic crafting is done by level 3
                if(playercontext.Level >= 3) {
                    ItemRecipe[] recipes = context.GetModule<ItemModule>().GetRecipes(itemid).OrderByDescending(r => r.Ingredients.Length).ToArray();
                    foreach(ItemRecipe recipe in recipes) {
                        if(context.GetModule<InventoryModule>().HasItems(playerid, recipe.Ingredients.Select(i => i.Item).ToArray())) {
                            context.GetModule<InventoryModule>().CraftItem(playerid, context.GetModule<ItemModule>().GetItems(recipe.Ingredients.Select(i => i.Item)).ToArray());
                            return;
                        }
                    }
                }
            }

            if(item.Type == ItemType.Consumable) {
                Player player = context.GetModule<PlayerModule>().GetExistingPlayer(playerid);
                if(item.HP < player.MaximumHP * 0.35) {
                    context.GetModule<ShopModule>().SellItem(playerid, item, quantity, context.GetModule<ShopModule>().IsInsultNecessaryToSell(playerid, itemid) ? 0.2 : 0.0);
                    return;
                }
            }
            else if (item.Type == ItemType.Weapon || item.Type == ItemType.Armor) {
                if (item.LevelRequirement > context.GetModule<PlayerModule>().GetLevel(playerid))
                    return;

                EquipmentBonus bonus = context.GetModule<EquipmentModule>().GetEquipmentBonus(playerid, item.GetTargetSlot());
                if((item.Type == ItemType.Weapon && bonus.Damage >= item.Damage) || (item.Type == ItemType.Armor && bonus.Armor >= item.Armor))
                    context.GetModule<ShopModule>().SellItem(playerid, item, quantity, context.GetModule<ShopModule>().IsInsultNecessaryToSell(playerid, itemid) ? 0.2 : 0.0);
                else {
                    EquipmentItem olditem = context.GetModule<EquipmentModule>().Equip(playerid, item, item.GetTargetSlot());
                    if(quantity > 1)
                        context.GetModule<ShopModule>().SellItem(playerid, item, quantity - 1, context.GetModule<ShopModule>().IsInsultNecessaryToSell(playerid, itemid) ? 0.2 : 0.0);
                    context.GetModule<ShopModule>().SellItem(playerid, olditem.ItemID, 1, context.GetModule<ShopModule>().IsInsultNecessaryToSell(playerid, olditem.ItemID) ? 0.2 : 0.0);
                }
                return;
            }

            context.GetModule<ShopModule>().SellItem(playerid, item, quantity, context.GetModule<ShopModule>().IsInsultNecessaryToSell(playerid, itemid) ? 0.2 : 0.0);

            if(playercontext.Level >= 3) {
                int sizethreshold = (int)(context.GetModule<InventoryModule>().GetMaximumInventorySize(playerid) * 0.8);

                if(context.GetModule<InventoryModule>().GetInventorySize(playerid) > sizethreshold) {
                    List<FullInventoryItem> inventory = new List<FullInventoryItem>(context.GetModule<InventoryModule>().GetInventoryItems(playerid));

                    while(inventory.Count > sizethreshold) {
                        FullInventoryItem sellitem = inventory.Where(i => i.Type == ItemType.Misc && i.Name != "Pee" && i.Name != "Poo").OrderBy(i => i.Value).FirstOrDefault();
                        if(sellitem == null)
                            sellitem = inventory.FirstOrDefault(i => i.Type == ItemType.Potion && i.HP == 0);
                        if(sellitem == null)
                            sellitem = inventory.Where(i => i.Type == ItemType.Consumable).OrderBy(i => i.Value).FirstOrDefault();
                        if(sellitem == null)
                            break;

                        context.GetModule<ShopModule>().SellItem(playerid, sellitem.ID, sellitem.Quantity, context.GetModule<ShopModule>().IsInsultNecessaryToSell(playerid, sellitem.ID) ? 0.2 : 0.0);
                        inventory.Remove(sellitem);
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
                context.GetModule<AdventureModule>().Rest(playerid);
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
                    if(context.GetModule<SkillModule>().CanCastHeal(playerid) || context.GetModule<InventoryModule>().HasHealingItems(playerid))
                        context.GetModule<ConvenienceModule>().Heal(playerid);
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
                    Level = context.GetModule<SkillModule>().GetSkillLevel(playerid, SkillType.Awareness)
                };

            else afkdetection.Remove(playerid);
        }
    }
}