using System;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Streaming.Collections.Management;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Ticker;

namespace StreamRC.Streaming.Collections {

    /// <summary>
    /// module providing collections for users
    /// </summary>
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(TickerModule), DependencyType.Type)]
    [Dependency(ModuleKeys.MainWindow, DependencyType.Key)]
    [ModuleKey("collection")]
    public class CollectionModule : IInitializableModule, IStreamCommandHandler, ICommandModule {
        readonly Context context;

        readonly CollectionTickerGenerator tickergenerator;

        /// <summary>
        /// creates a new <see cref="CollectionModule"/>
        /// </summary>
        /// <param name="context">module context</param>
        public CollectionModule(Context context) {
            this.context = context;
            tickergenerator = new CollectionTickerGenerator(context, this);
        }

        /// <summary>
        /// triggered when a collection was added
        /// </summary>
        public event Action<Collection> CollectionAdded;

        /// <summary>
        /// triggered when a collection was removed
        /// </summary>
        public event Action<Collection> CollectionRemoved;

        public event Action<Collection, CollectionItem> ItemAdded;

        public event Action<Collection, CollectionItem> ItemRemoved;

        public event Action<Collection, BlockedCollectionItem> ItemBlocked;

        public event Action<Collection, BlockedCollectionItem> ItemUnblocked;

        public event Action<Collection, string> CollectionClearedUser;

        void OnCollectionAdded(Collection collection) {
            CollectionAdded?.Invoke(collection);
            if(tickergenerator.CollectionCount == 1)
                context.GetModule<TickerModule>().AddSource(tickergenerator);
        }

        void OnCollectionRemoved(Collection collection) {
            CollectionRemoved?.Invoke(collection);
            if(tickergenerator.CollectionCount==0)
                context.GetModule<TickerModule>().RemoveSource(tickergenerator);
        }

        public Collection GetCollection(string name) {
            return context.Database.LoadEntities<Collection>().Where(c => c.Name == name).Execute().FirstOrDefault();
        }

        public long GetCollectionCount() {
            return context.Database.Load<Collection>(DBFunction.Count).ExecuteScalar<long>();
        }

        public Collection[] GetCollections() {
            return context.Database.LoadEntities<Collection>().Execute().ToArray();
        }

        public CollectionItem[] GetItems(string collection) {
            return context.Database.LoadEntities<CollectionItem>().Where(i => i.Collection == collection).Execute().ToArray();
        }

        public BlockedCollectionItem[] GetBlockedItems(string collection) {
            return context.Database.LoadEntities<BlockedCollectionItem>().Where(i => i.Collection == collection).Execute().ToArray();
        }

        public WeightedCollectionItem[] GetWeightedItems(string collection) {
            return context.Database.LoadEntities<WeightedCollectionItem>().Where(i=>i.Collection==collection).Execute().ToArray();
        }

        public void ProcessStreamCommand(StreamCommand command) {
            switch(command.Command) {
                case "add":
                    AddItem(command.User, command.Arguments[0].ToLower(), command.Arguments[1].ToLower());
                    break;
                case "remove":
                    RemoveItem(command.User, command.Arguments[0].ToLower(), command.Arguments[1].ToLower());
                    break;
                case "clear":
                    Clear(command.User, command.Arguments[0].ToLower());
                    break;
                case "collections":
                    ListCollections(command);
                    break;
                case "collectioninfo":
                    DisplayCollectionInfo(command);
                    break;
                default:
                    throw new StreamCommandException("Command not supported by this module");
            }
        }

        void DisplayCollectionInfo(StreamCommand command) {
            string collectionname = command.Arguments[0];
            Collection collection = context.Database.LoadEntities<Collection>().Where(c => c.Name == collectionname).Execute().FirstOrDefault();
            if (collection == null)
                throw new StreamCommandException($"There is no collection named '{collectionname}'");

            string itemsperuser = collection.ItemsPerUser > 0 ? $"max {collection.ItemsPerUser} per user." : "unlimited items per user";
            context.GetModule<StreamModule>().SendMessage(command.Service, command.User, $"Collection {collectionname}: {collection.Description} - {itemsperuser}", command.IsWhispered);
        }

        void ListCollections(StreamCommand command) {
            string message = string.Join(", ", context.Database.Load<Collection>(c => c.Name).ExecuteSet<string>());
            if(message.Length == 0)
                message = "There are no open collections";
            else message = "Open collections: " + message;

            context.GetModule<StreamModule>().SendMessage(command.Service, command.User, message, command.IsWhispered);
        }

        void Clear(string user, string collectionname) {
            Collection collection = context.Database.LoadEntities<Collection>().Where(c => c.Name == collectionname).Execute().FirstOrDefault();
            if (collection == null)
                throw new StreamCommandException($"There is no collection named '{collectionname}'");

            if(context.Database.Delete<CollectionItem>().Where(i => i.Collection == collectionname && i.User == user).Execute() == 0)
                throw new StreamCommandException($"You had no items in collection '{collectionname}'");

            Logger.Info(this, $"Collection '{collectionname}' was cleared of items by {user}");
            CollectionClearedUser?.Invoke(collection, user);
        }

        void RemoveItem(string user, string collectionname, string item) {
            Collection collection = context.Database.LoadEntities<Collection>().Where(c => c.Name == collectionname).Execute().FirstOrDefault();
            if (collection == null)
                throw new StreamCommandException($"There is no collection named '{collectionname}'");

            if(context.Database.Delete<CollectionItem>().Where(i => i.Collection == collectionname && i.User == user && i.Item == item).Execute() == 0)
                throw new StreamCommandException($"There is no item '{item}' in '{collectionname}' which was added by you.");

            Logger.Info(this, $"{item} of {user} was removed from {collectionname}");
            ItemRemoved?.Invoke(collection, new CollectionItem {
                Collection = collectionname,
                Item = item,
                User = user
            });
        }

        void AddItem(string user, string collectionname, string item) {
            Collection collection = context.Database.LoadEntities<Collection>().Where(c => c.Name == collectionname).Execute().FirstOrDefault();
            if(collection == null)
                throw new StreamCommandException($"There is no collection named '{collectionname}'");

            BlockedCollectionItem blocked = context.Database.LoadEntities<BlockedCollectionItem>().Where(i => i.Collection == collectionname && i.Item == item).Execute().FirstOrDefault();
            if(blocked != null)
                throw new StreamCommandException($"You can't add '{item}' to the collection '{collectionname}'. Reason: {blocked.Reason}");

            if(context.Database.Load<CollectionItem>(DBFunction.Count).Where(i => i.Collection == collectionname && i.User == user && i.Item == item).ExecuteScalar<int>() > 0)
                throw new StreamCommandException($"You already have added '{item}' to the collection '{collectionname}'");

            if(collection.ItemsPerUser > 0) {
                int currentitems = context.Database.Load<CollectionItem>(DBFunction.Count).Where(i => i.Collection == collectionname && i.User == user).ExecuteScalar<int>();
                if(currentitems >= collection.ItemsPerUser)
                    throw new StreamCommandException($"You already have {collection.ItemsPerUser} in the collection '{collectionname}' which is the maximum per user for that collection. Remove some items or wait until some of them were processed.");
            }

            context.Database.Insert<CollectionItem>().Columns(i => i.Collection, i => i.Item, i => i.User).Values(collectionname, item, user).Execute();

            Logger.Info(this, $"{item} was added to {collectionname} by {user}");
            ItemAdded?.Invoke(collection, new CollectionItem {
                Collection = collectionname,
                Item = item,
                User = user
            });
        }

        public string ProvideHelp(string command) {
            switch(command) {
                case "add":
                    return "Adds an item to a collection. Syntax: !add <collection> <item>";
                case "remove":
                    return "Removes a personal item from a collection. Syntax: !remove <collection> <item>";
                case "clear":
                    return "Clears all items personally added to a collection. Syntax: !clear <collection>";
                case "collections":
                    return "Lists all available collections. Syntax: !collections";
                case "collectioninfo":
                    return "Provides info about a collection. Syntax: !collectioninfo <collection>";
                default:
                    throw new StreamCommandException("Command not supported by this module");
            }
        }

        public void Initialize() {
            context.Database.UpdateSchema<Collection>();
            context.Database.UpdateSchema<CollectionItem>();
            context.Database.UpdateSchema<BlockedCollectionItem>();
            context.Database.UpdateSchema<WeightedCollectionItem>();

            if(tickergenerator.CollectionCount > 0)
                context.GetModule<TickerModule>().AddSource(tickergenerator);

            context.GetModule<StreamModule>().RegisterCommandHandler(this, "add", "remove", "clear", "collections", "collectioninfo");
            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Manage.Collections", (sender, args) => new CollectionManagementWindow(this).Show());
        }

        public void ProcessCommand(string command, string[] arguments) {
            switch(command.ToLower()) {
                case "create":
                    CreateCollection(arguments[0].ToLower(), arguments[1], arguments.Length > 2 ? int.Parse(arguments[2]) : 0);
                    break;
                case "remove":
                    RemoveCollection(arguments[0].ToLower());
                    break;
                case "block":
                    BlockItem(arguments[0].ToLower(), arguments[1].ToLower(), arguments[2]);
                    break;
                case "unblock":
                    UnblockItem(arguments[0].ToLower(), arguments[1].ToLower());
                    break;
            }
        }

        /// <summary>
        /// unblocks an item
        /// </summary>
        /// <param name="collectionname">name of the collection the item is blocked from</param>
        /// <param name="item">item to unblock</param>
        public void UnblockItem(string collectionname, string item) {
            if(context.Database.Delete<BlockedCollectionItem>().Where(i => i.Collection == collectionname && i.Item == item).Execute() == 0)
                throw new StreamCommandException($"No blocked item '{item}' found for collection '{collectionname}'");

            Logger.Info(this, $"{item} was unblocked of {collectionname}");
            ItemUnblocked?.Invoke(new Collection {
                Name = collectionname
            }, new BlockedCollectionItem {
                Collection = collectionname,
                Item = item
            });
        }

        /// <summary>
        /// blocks an item of a collection
        /// </summary>
        /// <param name="collectionname">name of the collection</param>
        /// <param name="item">item name</param>
        /// <param name="reason">reason for blockage</param>
        public void BlockItem(string collectionname, string item, string reason) {
            Collection collection = context.Database.LoadEntities<Collection>().Where(c => c.Name == collectionname).Execute().FirstOrDefault();
            if (collection == null)
                throw new StreamCommandException($"There is no collection named '{collectionname}'");

            if(context.Database.Update<BlockedCollectionItem>().Set(i => i.Reason == reason).Where(i => i.Collection == collectionname && i.Item == item).Execute() == 0)
                context.Database.Insert<BlockedCollectionItem>().Columns(i => i.Collection, i => i.Item, i => i.Reason).Values(collectionname, item, reason).Execute();
            context.Database.Delete<CollectionItem>().Where(i => i.Collection == collectionname && i.Item == item).Execute();

            Logger.Info(this, $"{item} was blocked from {collectionname}", reason);
            ItemBlocked?.Invoke(collection, new BlockedCollectionItem {
                Collection = collectionname,
                Item = item,
                Reason = reason
            });
        }

        /// <summary>
        /// removes a collection from database
        /// </summary>
        /// <param name="collectionname">name of collection</param>
        public void RemoveCollection(string collectionname) {
            context.Database.Delete<Collection>().Where(c => c.Name == collectionname).Execute();
            context.Database.Delete<BlockedCollectionItem>().Where(c => c.Collection == collectionname).Execute();
            context.Database.Delete<CollectionItem>().Where(c => c.Collection == collectionname).Execute();

            Logger.Info(this, $"{collectionname} was removed");
            OnCollectionRemoved(new Collection {
                Name = collectionname
            });
        }

        /// <summary>
        /// creates a new collection
        /// </summary>
        /// <param name="collectionname">name of collection</param>
        /// <param name="description">description for collection</param>
        /// <param name="numberofitemsperuser">number of items a single user can add</param>
        public void CreateCollection(string collectionname, string description, int numberofitemsperuser) {
            if(context.Database.Update<Collection>().Set(c => c.Description == description, c => c.ItemsPerUser == numberofitemsperuser).Where(c => c.Name == collectionname).Execute() == 0)
                context.Database.Insert<Collection>().Columns(c => c.Name, c => c.Description, c => c.ItemsPerUser).Values(collectionname, description, numberofitemsperuser).Execute();

            Logger.Info(this, $"Collection {collectionname} was created", $"{description}, {numberofitemsperuser} items per user");
            OnCollectionAdded(new Collection {
                Name = collectionname,
                Description = description,
                ItemsPerUser = numberofitemsperuser
            });
        }

        /// <summary>
        /// changes collection details
        /// </summary>
        /// <param name="oldname">old name of collection</param>
        /// <param name="newname">new name of collection</param>
        /// <param name="description">collection description</param>
        /// <param name="numberofitemsperuser">number of items user can add</param>
        public void ChangeCollection(string oldname, string newname, string description, int numberofitemsperuser) {
            Logger.Info(this, $"Collection {oldname} was changed", $"{newname}, {description}, {numberofitemsperuser} items per user");
            context.Database.Update<Collection>().Set(c => c.Name == newname, c => c.Description == description, c => c.ItemsPerUser == numberofitemsperuser).Where(c => c.Name == oldname).Execute();

            if(oldname != newname) {
                context.Database.Update<CollectionItem>().Set(i=>i.Collection==newname).Where(i=>i.Collection==oldname).Execute();
                context.Database.Update<BlockedCollectionItem>().Set(i=>i.Collection==newname).Where(i=>i.Collection==oldname).Execute();
            }
        }
    }
}