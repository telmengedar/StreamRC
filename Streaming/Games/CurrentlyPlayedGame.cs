namespace StreamRC.Streaming.Games {

    /// <summary>
    /// structure which contains the currently played game
    /// </summary>
    public class CurrentlyPlayedGame {

        /// <summary>
        /// name of game
        /// </summary>
        public string Game { get; set; }

        /// <summary>
        /// epithet of game
        /// </summary>
        public string Epithet { get; set; }

        /// <summary>
        /// system
        /// </summary>
        public string System { get; set; }

        /// <summary>
        /// year game was released
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// link to game description
        /// </summary>
        public string MobyGames { get; set; } 
    }
}