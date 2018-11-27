using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Data;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Randoms;
using NightlyCode.Database.Entities.Operations;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Scripts;
using StreamRC.RPG.Items.Recipes;
using StreamRC.Streaming.Stream;

namespace StreamRC.RPG.Items {

    /// <summary>
    /// module managing game items
    /// </summary>
    [Module]
    public class ItemModule {
        readonly DatabaseModule database;
        readonly IStreamModule stream;
        ItemRecipe[] recipes=new ItemRecipe[0];

        /// <summary>
        /// creates a new <see cref="ItemModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public ItemModule(DatabaseModule database, IStreamModule stream) {
            database.Database.UpdateSchema<Item>();
            this.database = database;
            this.stream = stream;

            Task.Run(() => InitializeItems());
        }

        public long GetRandomItemID() {
            return database.Database.Load<Item>(i => i.ID).Where(i => i.Type != ItemType.Gold).OrderBy(new OrderByCriteria(DBFunction.Random)).Limit(1).ExecuteScalar<long>();
        }

        /// <summary>
        /// determines whether any recipe contains an item as an ingredient
        /// </summary>
        /// <param name="itemid">id of item</param>
        /// <returns>true when a recipe exists which contains this item as an ingredient, false otherwise</returns>
        public bool IsIngredient(long itemid) {
            return recipes.Any(r => r.Ingredients.Any(i => i.Item == itemid));
        }

        /// <summary>
        /// returns all recipes which contain the specified item
        /// </summary>
        /// <param name="itemid">id of item to be checked</param>
        /// <returns>recipes</returns>
        public IEnumerable<ItemRecipe> GetRecipes(long itemid) {
            return recipes.Where(r => r.Ingredients.Any(i => i.Item == itemid));
        }

        public IEnumerable<Item> GetItems(IEnumerable<long> itemids) {
            return database.Database.LoadEntities<Item>().Where(i => itemids.Contains(i.ID)).Execute();
        }

        /// <summary>
        /// get a compatible recipe for the specified ingredients
        /// </summary>
        /// <param name="ingredients">ingredients to use</param>
        /// <returns>recipe which matches the ingredients, null if no recipe is found</returns>
        public ItemRecipe GetRecipe(long[] ingredients) {
            return recipes.FirstOrDefault(r => r.Ingredients.Length == ingredients.Length && ingredients.All(i => r.Ingredients.Any(ri => ri.Item == i)) && r.Ingredients.All(i => ingredients.Any(ri => ri == i.Item)));
        }

        /// <summary>
        /// recognizes an item in an argument list
        /// </summary>
        /// <param name="arguments">arguments to search for items</param>
        /// <returns></returns>
        public Item RecognizeItem(string[] arguments) {
            int startindex = 0;
            return RecognizeItem(arguments, ref startindex);
        }

        /// <summary>
        /// recognizes an item in an argument list
        /// </summary>
        /// <param name="arguments">arguments to search for items</param>
        /// <param name="startindex">index in argument list where to start looking</param>
        /// <returns></returns>
        public Item RecognizeItem(string[] arguments, ref int startindex)
        {
            for(int i = arguments.Length; i > startindex; --i) {
                string name = string.Join(" ", arguments.Skip(startindex).Take(i - startindex));

                foreach(string singlename in name.GetPossibleSingular()) {
                    Item item = GetItem(singlename);
                    if(item != null) {
                        startindex = i;
                        return item;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// get id of item by name
        /// </summary>
        /// <param name="name">name of item</param>
        /// <returns>id of item</returns>
        public long GetItemID(string name) {
            return database.Database.Load<Item>(i => i.ID).Where(i => i.Name == name).ExecuteScalar<long>();
        }

        /// <summary>
        /// get id of item by name
        /// </summary>
        /// <param name="name">name of item</param>
        /// <returns>id of item</returns>
        public long GetItemID(string[] name)
        {
            return GetItemID(string.Join(" ", name));
        }

        /// <summary>
        /// get item from database by name
        /// </summary>
        /// <param name="name">name of item</param>
        /// <returns>item information</returns>
        public Item GetItem(string name) {
            return database.Database.LoadEntities<Item>().Where(i => i.Name.ToLower() == name.ToLower()).Execute().FirstOrDefault();
        }

        /// <summary>
        /// get item from database by name
        /// </summary>
        /// <param name="name">name of item</param>
        /// <returns>item information</returns>
        public Item GetItem(string[] name) {
            return GetItem(string.Join(" ", name));
        }

        /// <summary>
        /// get item from database by id
        /// </summary>
        /// <param name="itemid">id of item</param>
        /// <returns>item information</returns>
        public Item GetItem(long itemid) {
            return database.Database.LoadEntities<Item>().Where(i => i.ID == itemid).Execute().FirstOrDefault();
        }

        /// <summary>
        /// selects an item randomly
        /// </summary>
        /// <param name="level">player level</param>
        /// <param name="luck">player luck</param>
        /// <returns>random item</returns>
        public Item SelectItem(int level, int luck) {
            Item[] items = database.Database.LoadEntities<Item>().Where(i => i.FindRequirement <= level).Execute().ToArray();
            return items.RandomItem(i => Math.Min(1.0, (double)luck/Math.Max(1, i.FindOptimum)), RNG.XORShift64);
        }

        void InitializeItems() {
            StringBuilder log=new StringBuilder();

            foreach(Item resourceitem in DataTable.ReadCSV(ResourceAccessor.GetResource<Stream>(GetType().Namespace + ".items.csv"), '\t', true).Deserialize<Item>()) {
                Item databaseitem = database.Database.LoadEntities<Item>().Where(i => i.Name == resourceitem.Name).Execute().FirstOrDefault();
                if(databaseitem == null) {
                    database.Database.Insert<Item>()
                        .Columns(i => i.Name, i => i.Type, i => i.Handedness, i => i.Target, i => i.FindRequirement, i => i.FindOptimum, i => i.UsageOptimum, i => i.Armor, i => i.Damage, i => i.Value, i => i.LevelRequirement, i => i.HP, i => i.MP, i=>i.Countable, i=>i.Pee, i=>i.Poo, i=>i.IsFluid, i=>i.Command, i=>i.CommandMessage, i=>i.CriticalAdjective)
                        .Values(resourceitem.Name, resourceitem.Type, resourceitem.Handedness, resourceitem.Target, resourceitem.FindRequirement, resourceitem.FindOptimum, resourceitem.UsageOptimum, resourceitem.Armor, resourceitem.Damage, resourceitem.Value, resourceitem.LevelRequirement, resourceitem.HP, resourceitem.MP, resourceitem.Countable, resourceitem.Pee, resourceitem.Poo, resourceitem.IsFluid, resourceitem.Command, resourceitem.CommandMessage, resourceitem.CriticalAdjective)
                        .Execute();
                    log.AppendLine($"'{resourceitem.Name}' added to database.");
                }
                else {
                    if(!databaseitem.Equals(resourceitem)) {
                        database.Database.Update<Item>().Set(i => i.Type == resourceitem.Type, i => i.Handedness == resourceitem.Handedness, i => i.Target == resourceitem.Target, i => i.FindRequirement == resourceitem.FindRequirement, i => i.FindOptimum == resourceitem.FindOptimum, i => i.UsageOptimum == resourceitem.UsageOptimum, i => i.Armor == resourceitem.Armor, i => i.Damage == resourceitem.Damage, i => i.Value == resourceitem.Value, i => i.LevelRequirement == resourceitem.LevelRequirement, i => i.HP == resourceitem.HP, i => i.MP == resourceitem.MP, i=>i.Countable==resourceitem.Countable, i=>i.Pee==resourceitem.Pee, i=>i.Poo==resourceitem.Poo, i=>i.IsFluid==resourceitem.IsFluid, i=>i.Command==resourceitem.Command, i=>i.CommandMessage==resourceitem.CommandMessage, i=>i.CriticalAdjective==resourceitem.CriticalAdjective).Where(i => i.ID == databaseitem.ID).Execute();
                        log.AppendLine($"'{resourceitem.Name}' updated");
                    }
                }
            }

            recipes = JSON.Read<ResourceRecipe[]>(ResourceAccessor.GetResource<Stream>("StreamRC.RPG.Items.Recipes.recipes.json")).Select(r => new ItemRecipe {
                ItemID = GetItemID(r.Result),
                Ingredients = r.Ingredients.Select(i => new RecipeIngredient {
                    Item = GetItemID(i.Item),
                    Consumed = i.Consumed
                }).ToArray()
            }).ToArray();

            if(log.Length>0)
                Logger.Info(this, "Item Information changed", log.ToString());
        }

        [Command("iteminfo", "$service", "$channel", "$user")]
        public void PrintItemInfo(string service, string channel, string user, string[] arguments) {
            if(arguments.Length == 0) {
                stream.SendMessage(service, channel, user, "You have to provide the name of the item to get info about.");
                return;
            }

            Item item = GetItem(arguments);
            if(item == null) {
                stream.SendMessage(service, channel, user, $"There is no item by the name {string.Join(" ", arguments)}.");
                return;
            }

            StringBuilder iteminfo = new StringBuilder(item.Name).Append(": ");
            switch(item.Type) {
                case ItemType.Armor:
                    iteminfo.Append($"Armor {item.Armor} ");
                    break;
                case ItemType.Weapon:
                    iteminfo.Append($"Damage {item.Damage} ");
                    break;
                case ItemType.Consumable:
                    iteminfo.Append($"Heals {item.HP} HP and {item.MP} MP ");
                    break;
            }

            iteminfo.Append($"Base Value {item.Value}");

            if(item.LevelRequirement > 0)
                iteminfo.Append($" Required Level: {item.LevelRequirement}");

            stream.SendMessage(service, channel, user, iteminfo.ToString());
        }
    }
}