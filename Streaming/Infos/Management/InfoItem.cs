using System.ComponentModel;
using System.Runtime.CompilerServices;
using NightlyCode.StreamRC.Properties;

namespace StreamRC.Streaming.Infos.Management {

    /// <summary>
    /// item representing info in <see cref="InfoManagementWindow"/>
    /// </summary>
    public class InfoItem : Info, INotifyPropertyChanged {
        string key;
        string text;

        /// <summary>
        /// creates a new <see cref="InfoItem"/>
        /// </summary>
        public InfoItem() { }

        /// <summary>
        /// creates a new <see cref="InfoItem"/>
        /// </summary>
        /// <param name="info">info used to fill item information</param>
        public InfoItem(Info info) {
            Key = info.Key;
            Text = info.Text;
            Apply();
        }

        /// <summary>
        /// key of info before it was changed
        /// </summary>
        public string OldKey => base.Key;

        /// <summary>
        /// text of info before it was changed
        /// </summary>
        public string OldText => base.Text;

        /// <summary>
        /// key used to identify info
        /// </summary>
        public new string Key
        {
            get { return key; }
            set
            {
                if(value == key) return;
                key = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// info text
        /// </summary>
        public new string Text
        {
            get { return text; }
            set
            {
                if(value == text) return;
                text = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// applies changes to item
        /// </summary>
        public void Apply() {
            base.Key = Key;
            base.Text = Text;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}