using System.Collections.Generic;
using StreamRC.Gambling.Cards;

namespace StreamRC.Gambling.BlackJack {

    /// <summary>
    /// a single game of black jack
    /// </summary>
    public class BlackJackGame {

        /// <summary>
        /// id of player
        /// </summary>
        public long PlayerID { get; set; }

        /// <summary>
        /// card stack from which playing cards are drawn
        /// </summary>
        public CardStack Stack { get; set; }

        /// <summary>
        /// board of dealer
        /// </summary>
        public Board DealerBoard { get; set; }

        /// <summary>
        /// boards of player
        /// </summary>
        public List<BlackJackBoard> PlayerBoards { get; set; }

        /// <summary>
        /// actively played board of player
        /// </summary>
        public int ActiveBoard { get; set; }
    }
}