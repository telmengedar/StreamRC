using System.ComponentModel;
using System.Runtime.CompilerServices;
using NightlyCode.StreamRC.Properties;

namespace StreamRC.Streaming.Collections.Management {

    /// <summary>
    /// item used to edit <see cref="Collection"/>s
    /// </summary>
    public class CollectionEditor : Collection, INotifyPropertyChanged {
        string name;
        string description;
        int itemsPerUser;

        public CollectionEditor() { }

        public CollectionEditor(Collection collection) {
            Name = collection.Name;
            Description = collection.Description;
            ItemsPerUser = collection.ItemsPerUser;
            Apply();
        }

        public string OldName => base.Name;

        public string OldDescription => base.Description;

        public int OldItemsPerUser => base.ItemsPerUser;

        /// <summary>
        /// name of collection
        /// </summary>
        public new string Name
        {
            get { return name; }
            set
            {
                if(value == name) return;
                name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// description for collection
        /// </summary>
        public new string Description
        {
            get { return description; }
            set
            {
                if(value == description) return;
                description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// items a single user can add
        /// </summary>
        public new int ItemsPerUser
        {
            get { return itemsPerUser; }
            set
            {
                if(value == itemsPerUser) return;
                itemsPerUser = value;
                OnPropertyChanged();
            }
        }

        public void Apply() {
            base.Name = Name;
            base.Description = Description;
            base.ItemsPerUser = ItemsPerUser;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}