using StreamRC.Gambling.Cards;

namespace StreamRC.Gambling.Poker.Draw {

    /// <summary>
    /// context for a video poker game
    /// </summary>
    public class VideoPokerGame {

        /// <summary>
        /// deck of cards used
        /// </summary>
        public CardStack Deck { get; set; }

        /// <summary>
        /// hand of player
        /// </summary>
        public Board Hand { get; set; }

        /// <summary>
        /// amount player has bet
        /// </summary>
        public int Bet { get; set; }

        /// <summary>
        /// determines whether bet equals max bet for player
        /// </summary>
        public bool IsMaxBet { get; set; }
    }
}