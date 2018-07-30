using System;
using System.Linq;
using System.Windows;
using NightlyCode.Core.Collections;
using StreamRC.Streaming.Polls.Management;
using StreamRC.Streaming.Statistics;

namespace StreamRC.Streaming.Collections.Management
{
    /// <summary>
    /// Interaction logic for PollManagementWindow.xaml
    /// </summary>
    public partial class CollectionManagementWindow : Window {
        readonly CollectionModule module;

        readonly NotificationList<CollectionEditor> collections=new NotificationList<CollectionEditor>();
        readonly NotificationList<BlockedItemEditor> blockeditems=new NotificationList<BlockedItemEditor>();
        readonly NotificationList<CollectionItem> items=new NotificationList<CollectionItem>();

        string selectedcollection = null;

        /// <summary>
        /// creates a new <see cref="Statistics.PollManagementWindow"/>
        /// </summary>
        /// <param name="module">collection module</param>
        public CollectionManagementWindow(CollectionModule module) {
            this.module = module;
            InitializeComponent();

            module.CollectionAdded += OnCollectionAdded;
            module.CollectionRemoved += OnCollectionRemoved;
            module.ItemAdded += OnItemAdded;
            module.ItemRemoved += OnItemRemoved;
            module.CollectionClearedUser += OnCollectionCleared;
            module.ItemBlocked += OnItemBlocked;
            module.ItemUnblocked += OnItemUnblocked;

            collections.ItemChanged += OnEditCollectionItemChanged;

            foreach(Collection collection in module.GetCollections())
                collections.Add(new CollectionEditor(collection));

            grdCollections.ItemsSource = collections;
            grdBlockedItems.ItemsSource = blockeditems;
            grdItems.ItemsSource = items;
        }

        void OnEditCollectionItemChanged(CollectionEditor collection, string property) {
            switch(property) {
                case "Name":
                    if(string.IsNullOrEmpty(collection.OldName))
                        module.CreateCollection(collection.Name, collection.Description, collection.ItemsPerUser);
                    else module.ChangeCollection(collection.OldName, collection.Name, collection.Description, collection.ItemsPerUser);
                    selectedcollection = collection.Name;
                    items.Foreach(i => i.Collection = collection.Name);
                    blockeditems.Foreach(i => i.Collection = collection.Name);
                    break;
                default:
                    module.ChangeCollection(collection.OldName, collection.Name, collection.Description, collection.ItemsPerUser);
                    break;
            }
            collection.Apply();
        }

        void OnItemUnblocked(Collection collection, BlockedCollectionItem item) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(item.Collection != selectedcollection)
                    return;

                blockeditems.RemoveWhere(i => i.Item == item.Item);
            }));
        }

        void OnItemBlocked(Collection collection, BlockedCollectionItem item) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(item.Collection != selectedcollection || blockeditems.Any(i=>i.Item==item.Item))
                    return;

                blockeditems.RemoveWhere(i => i.Item == item.Item);
                blockeditems.Add(new BlockedItemEditor(item));
            }));
        }

        void OnCollectionCleared(Collection collection, string user) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(collection.Name != selectedcollection)
                    return;

                items.RemoveWhere(i => i.User == user);
            }));
        }

        void OnItemRemoved(Collection collection, CollectionItem item) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(collection.Name != selectedcollection)
                    return;

                items.RemoveWhere(i => i.User == item.User && i.Item == item.Item);
            }));
        }

        void OnItemAdded(Collection collection, CollectionItem item) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(collection.Name != selectedcollection || items.Any(i => i.User == item.User && i.Item == item.Item))
                    return;

                items.Add(item);
            }));
        }

        void OnCollectionRemoved(Collection collection) {
            Dispatcher.BeginInvoke((Action)(() => {
                collections.RemoveWhere(c => c.Name == collection.Name);
            }));
        }

        void OnCollectionAdded(Collection collection) {
            Dispatcher.BeginInvoke((Action)(() => {
                if (collections.Any(c => c.Name == collection.Name))
                    return;

                collections.Add(new CollectionEditor(collection));
            }));
        }

        void grdCollections_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            Collection collection = grdCollections.SelectedItem as Collection;
            if (collection == null) {
                selectedcollection = null;
                items.Clear();
                blockeditems.Clear();
                ctxRemoveCollection.IsEnabled = false;
            }
            else {
                selectedcollection = collection.Name;

                items.Clear();
                blockeditems.Clear();

                foreach(CollectionItem item in module.GetItems(collection.Name))
                    items.Add(item);
                foreach(BlockedCollectionItem item in module.GetBlockedItems(collection.Name))
                    blockeditems.Add(new BlockedItemEditor(item));

                ctxRemoveCollection.IsEnabled = true;
            }
        }

        void Context_BlockItem(object sender, RoutedEventArgs e) {
            CollectionItem item = grdItems.SelectedItem as CollectionItem;
            if(item == null)
                return;

            module.BlockItem(item.Collection, item.Item, "");
        }

        void Context_UnblockItem(object sender, RoutedEventArgs e) {
            BlockedItemEditor item = grdBlockedItems.SelectedItem as BlockedItemEditor;
            if (item == null)
                return;

            module.UnblockItem(item.Collection, item.Item);
        }

        void Context_RemoveCollection(object sender, RoutedEventArgs e) {
            CollectionEditor collection=grdCollections.SelectedItem as CollectionEditor;
            if(collection == null)
                return;

            module.RemoveCollection(collection.Name);
        }

        void grdBlockedItems_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            BlockedItemEditor item = grdBlockedItems.SelectedItem as BlockedItemEditor;
            ctxUnblockItem.IsEnabled = item != null;
        }

        void grdItems_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CollectionItem item = grdItems.SelectedItem as CollectionItem;
            ctxBlockItem.IsEnabled = item != null;
        }
    }
}
