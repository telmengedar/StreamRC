namespace StreamRC.Gambling.Cards
{

    /// <summary>
    /// card in a deck
    /// </summary>
    public struct Card
    {
        readonly byte code;

        /// <summary>
        /// creates a new <see cref="Card"/>
        /// </summary>
        /// <param name="code">code from which card is built</param>
        public Card(byte code) {
            this.code = code;
        }

        /// <summary>
        /// creates a new <see cref="Card"/>
        /// </summary>
        /// <param name="rank">rank of card</param>
        /// <param name="suit">suit of card</param>
        public Card(CardRank rank, CardSuit suit)
        {
            code = (byte)(((int)rank << 2) + (int)suit);
        }

        public ulong Mask {
            get { return (ulong)(1L << ((code & 3) * 13) + (code >> 2)); }
        }

        public byte Code {
            get { return code; }
        }

        public CardRank Rank
        {
            get { return (CardRank)(code>>2); }
        }

        public CardSuit Suit
        {
            get { return (CardSuit)(code&3); }
        }

        public override int GetHashCode() {
            return code;
        }

        public override bool Equals(object obj)
        {
            return ((Card)obj).code == code;
        }

        /// <summary>
        /// parses a <see cref="Card"/> from string
        /// </summary>
        /// <param name="card">card in string format to be parsed</param>
        /// <returns>parsed card</returns>
        public static Card FromString(string card)
        {
            if (card.Length < 2) return new Card();
            CardRank rank;
            CardSuit suit;
            switch(card[0])
            {
                case '2': rank = CardRank.Deuce; break;
                case '3': rank = CardRank.Trey; break;
                case '4': rank = CardRank.Four; break;
                case '5': rank = CardRank.Five; break;
                case '6': rank = CardRank.Six; break;
                case '7': rank = CardRank.Seven; break;
                case '8': rank = CardRank.Eight; break;
                case '9': rank = CardRank.Nine; break;
                case 'T': rank = CardRank.Ten; break;
                case 'J': rank = CardRank.Jack; break;
                case 'Q': rank = CardRank.Queen; break;
                case 'K': rank = CardRank.King; break;
                case 'A': rank = CardRank.Ace; break;
                default: return new Card();
            }

            switch(card[1])
            {
                case 'c': suit = CardSuit.Clubs; break;
                case 'd': suit = CardSuit.Diamonds; break;
                case 'h': suit = CardSuit.Hearts; break;
                case 's': suit = CardSuit.Spades; break;
                default: return new Card();
            }

            return new Card(rank, suit);
        }

        public static char GetRankChar(CardRank rank)
        {
            switch (rank)
            {
                case CardRank.Deuce: return '2';
                case CardRank.Trey: return '3';
                case CardRank.Four: return '4';
                case CardRank.Five: return '5';
                case CardRank.Six: return '6'; 
                case CardRank.Seven: return '7';
                case CardRank.Eight: return '8';
                case CardRank.Nine: return '9';
                case CardRank.Ten: return 'T'; 
                case CardRank.Jack: return 'J';
                case CardRank.Queen: return 'Q';
                case CardRank.King: return 'K';
                case CardRank.Ace: return 'A';
                default: return '?';
            }
        }

        public override string ToString()
        {
            char rankchar = GetRankChar(Rank);
            char suitchar;

            switch(Suit)
            {
                case CardSuit.Clubs: suitchar = 'c'; break;
                case CardSuit.Diamonds: suitchar = 'd'; break;
                case CardSuit.Hearts: suitchar = 'h'; break;
                case CardSuit.Spades: suitchar = 's'; break;
                default: suitchar = '?'; break;
            }
            return $"{rankchar}{suitchar}";
        }
    }
}
