using StreamRC.Gambling.Cards;

namespace StreamRC.Gambling.BlackJack {

    /// <summary>
    /// board of player in a black jack game
    /// </summary>
    public class BlackJackBoard {

        /// <summary>
        /// cards building the board
        /// </summary>
        public Board Board { get; set; }

        /// <summary>
        /// bet for board
        /// </summary>
        public int Bet { get; set; }
    }
}