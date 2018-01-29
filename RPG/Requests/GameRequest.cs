using System;

namespace StreamRC.RPG.Requests {
    public class GameRequest {
        public long UserID { get; set; }
        public string Platform { get; set; }
        public string Game { get; set; }
        public string Conditions { get; set; }
    }
}