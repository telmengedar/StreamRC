using System.ComponentModel;
using System.Runtime.CompilerServices;
using NightlyCode.StreamRC.Properties;

namespace StreamRC.Streaming.Collections.Management {
    public class BlockedItemEditor : BlockedCollectionItem, INotifyPropertyChanged {
        string item;
        string reason;

        public BlockedItemEditor() { }

        public BlockedItemEditor(BlockedCollectionItem item) {
            Collection = item.Collection;
            Item = item.Item;
            Reason = item.Reason;
            Apply();
        }

        public string OldItem => base.Item;

        public string OldReason => base.Reason;

        /// <summary>
        /// name of the blocked item
        /// </summary>
        public new string Item
        {
            get { return item; }
            set
            {
                if(value == item) return;
                item = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// reason the item is blocked
        /// </summary>
        public new string Reason
        {
            get { return reason; }
            set
            {
                if(value == reason) return;
                reason = value;
                OnPropertyChanged();
            }
        }

        public void Apply() {
            base.Item = Item;
            base.Reason = Reason;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}