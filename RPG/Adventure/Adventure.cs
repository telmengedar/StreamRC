namespace StreamRC.RPG.Adventure {
    public class Adventure {

        public Adventure(long playerid) {
            Player = playerid;
        }

        public void Reset() {
            switch(AdventureLogic.Status) {
                case AdventureStatus.Exploration:
                    Cooldown = 100.0;
                    break;
                case AdventureStatus.SpiritRealm:
                    Cooldown = 10.0;
                    break;
                case AdventureStatus.MonsterBattle:
                    Cooldown = 8.0;
                    break;
            }
            
        }

        public double Cooldown { get; set; }

        public long Player { get; set; }

        public IAdventureLogic AdventureLogic { get; set; }
    }
}