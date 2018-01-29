using System.ComponentModel;
using System.Runtime.CompilerServices;
using NightlyCode.StreamRC.Properties;

namespace StreamRC.Streaming.Games {

    /// <summary>
    /// game to be played
    /// </summary>
    public class Game : INotifyPropertyChanged{
        string name;

        /// <summary>
        /// name of game
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if(value == name) return;

                name = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}