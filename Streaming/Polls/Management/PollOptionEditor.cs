using System.ComponentModel;
using System.Runtime.CompilerServices;
using NightlyCode.StreamRC.Properties;

namespace StreamRC.Streaming.Polls.Management {
    public class PollOptionEditor : PollOption, INotifyPropertyChanged {
        string key;
        string description;
        bool locked;

        public PollOptionEditor() { }

        public PollOptionEditor(PollOption option) {
            Poll = option.Poll;
            Key = option.Key;
            Description = option.Description;
            Locked = option.Locked;
            Apply();
        }

        public string OldKey => base.Key;

        public string OldDescription => base.Description;

        public bool OldLocked => base.Locked;

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

        public new bool Locked
        {
            get { return locked; }
            set
            {
                if(value == locked)
                    return;
                locked = value;
                OnPropertyChanged();
            }
        }

        public void Apply() {
            base.Key = Key;
            base.Description = Description;
            base.Locked = locked;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}