using System.ComponentModel;
using System.Runtime.CompilerServices;
using NightlyCode.Database.Entities;
using StreamRC.RPG.Annotations;
using StreamRC.RPG.Data;

namespace StreamRC.RPG.Players {
    public class PlayerInstance : INotifyPropertyChanged {
        readonly Player player;
        readonly IEntityManager entitymanager;

        int level;
        int gold;
        float experience;
        int currentHp;
        int currentMp;
        int maximumHp;
        int maximumMp;
        int strength;
        int dexterity;
        int fitness;
        int luck;

        public PlayerInstance(Player player, IEntityManager entitymanager) {
            this.player = player;
            this.entitymanager = entitymanager;
        }

        /// <summary>
        /// player level
        /// </summary>
        public int Level
        {
            get { return level; }
            set
            {
                if(level == value)
                    return;

                level = value;
                entitymanager.Update<Player>().Set(p => p.Level == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// collected gold
        /// </summary>
        public int Gold
        {
            get { return gold; }
            set
            {
                if(value == gold)
                    return;

                gold = value;
                entitymanager.Update<Player>().Set(p => p.Gold == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// user experience
        /// </summary>
        public float Experience
        {
            get { return experience; }
            set
            {
                if(value.Equals(experience)) return;

                experience = value;
                entitymanager.Update<Player>().Set(p => p.Experience == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// current health of player
        /// </summary>
        public int CurrentHP
        {
            get { return currentHp; }
            set
            {
                if(value == currentHp) return;
                currentHp = value;
                entitymanager.Update<Player>().Set(p => p.CurrentHP == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// current mana of player
        /// </summary>
        public int CurrentMP
        {
            get { return currentMp; }
            set
            {
                if(value == currentMp) return;
                currentMp = value;
                entitymanager.Update<Player>().Set(p => p.CurrentMP == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// maximum health of player
        /// </summary>
        public int MaximumHP
        {
            get { return maximumHp; }
            set
            {
                if(value == maximumHp) return;
                maximumHp = value;
                entitymanager.Update<Player>().Set(p => p.MaximumHP == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// maximum mana of player
        /// </summary>
        public int MaximumMP
        {
            get { return maximumMp; }
            set
            {
                if(value == maximumMp) return;
                maximumMp = value;
                entitymanager.Update<Player>().Set(p => p.MaximumMP == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// hit power
        /// </summary>
        public int Strength
        {
            get { return strength; }
            set
            {
                if(value == strength) return;
                strength = value;
                entitymanager.Update<Player>().Set(p => p.Strength == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// hit rate
        /// </summary>
        public int Dexterity
        {
            get { return dexterity; }
            set
            {
                if(value == dexterity) return;
                dexterity = value;
                entitymanager.Update<Player>().Set(p => p.Dexterity == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// how much stamina recovers
        /// </summary>
        public int Fitness
        {
            get { return fitness; }
            set
            {
                if(value == fitness) return;
                fitness = value;
                entitymanager.Update<Player>().Set(p => p.Fitness == value).Where(p => p.UserID == player.UserID).Execute();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// find something and good stuff
        /// </summary>
        public int Luck
        {
            get { return luck; }
            set
            {
                if(value == luck) return;
                luck = value;
                entitymanager.Update<Player>().Set(p => p.Fitness == value).Where(p => p.UserID == player.UserID).Execute();
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