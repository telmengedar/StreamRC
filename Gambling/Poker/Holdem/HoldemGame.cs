using StreamRC.Gambling.Cards;

namespace StreamRC.Gambling.Poker.Holdem {

    /// <summary>
    /// context for a casino holdem game
    /// </summary>
    public class HoldemGame {

        /// <summary>
        /// deck of cards
        /// </summary>
        public CardStack Deck { get; set; }

        /// <summary>
        /// mucked cards
        /// </summary>
        public CardStack Muck { get; set; }

        /// <summary>
        /// hand of dealer
        /// </summary>
        public Board DealerHand { get; set; }

        /// <summary>
        /// hand of player
        /// </summary>
        public Board PlayerHand { get; set; }

        /// <summary>
        /// shared board
        /// </summary>
        public Board Board { get; set; }

        /// <summary>
        /// id of player
        /// </summary>
        public long PlayerID { get; set; }

        /// <summary>
        /// bet amount for a single round
        /// </summary>
        public int Bet { get; set; }

        /// <summary>
        /// amount of gold in pot
        /// </summary>
        public int Pot { get; set; }
    }
}