namespace StreamRC.Gambling.Roulette {

    /// <summary>
    /// placed bet in roulette
    /// </summary>
    public class RouletteBet {

        /// <summary>
        /// user who placed the bet
        /// </summary>
        public long UserID { get; set; }

        /// <summary>
        /// bet amount
        /// </summary>
        public int Gold { get; set; }

        /// <summary>
        /// type of bet
        /// </summary>
        public BetType Type { get; set; }

        /// <summary>
        /// parameter for bet (number, red, black etc.)
        /// </summary>
        public int BetParameter { get; set; }
    }
}