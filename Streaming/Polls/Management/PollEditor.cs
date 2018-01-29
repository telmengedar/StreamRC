using System.ComponentModel;
using System.Runtime.CompilerServices;
using NightlyCode.StreamRC.Properties;

namespace StreamRC.Streaming.Polls.Management {
    public class PollEditor : Poll, INotifyPropertyChanged {
        string description;
        string name;

        public PollEditor() { }

        public PollEditor(Poll poll) {
            Name = poll.Name;
            Description = poll.Description;
            Apply();
        }

        public string OldName => base.Name;

        public string OldDescription => base.Name;

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

        public void Apply() {
            base.Name = Name;
            base.Description = Description;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}