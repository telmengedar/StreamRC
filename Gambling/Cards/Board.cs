using System.Collections.Generic;
using System.Text;

namespace StreamRC.Gambling.Cards {

    /// <summary>
    /// a board consisting out of cards
    /// </summary>
    public struct Board : IEnumerable<Card> {
        ulong code;

        /// <summary>
        /// creates a new <see cref="Board"/>
        /// </summary>
        /// <param name="code"></param>
        public Board(ulong code) {
            this.code = code;
        }

        /// <summary>
        /// creates a new <see cref="Board"/>
        /// </summary>
        /// <param name="cards"></param>
        public Board(params Card[] cards) {
            code = 0;
            AddRange(cards);
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="cards"></param>
        public Board(IEnumerable<Card> cards) {
            code = 0;
            AddRange(cards);
        }

        /// <summary>
        /// build a board from a cardmask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static Board FromMask(ulong mask) {
            Board board = new Board();
            for(int rank = 0; rank < 13; ++rank)
                for(int suit = 0; suit < 4; ++suit) {
                    ulong cardcode = (ulong)(1L << ((suit & 3) * 13) + (rank >> 2));
                    if((mask & cardcode) > 0)
                        board.Add(new Card((CardRank)rank, (CardSuit)suit));
                }
            return board;
        }

        /// <summary>
        /// indexer providing access to the cards
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Card this[int index] {
            get { return new Card((byte)((code >> (index * 6)) & 63)); }
            set { code = (code & ~(63UL << (index * 6))) | ((ulong)value.Code << (index * 6)); }
        }

        /// <summary>
        /// changes a card of the board
        /// </summary>
        /// <param name="index">index of card to change</param>
        /// <param name="card">new card value</param>
        /// <returns></returns>
        public Board ChangeCard(int index, Card card) {
            return new Board((code & ~(63UL << (index * 6))) | ((ulong)card.Code << (index * 6)));
        }

        /// <summary>
        /// clears the board
        /// </summary>
        public void Clear() {
            code = 0;
        }

        /// <summary>
        /// get the hand mask of the board
        /// </summary>
        /// <returns></returns>
        public ulong GetHandMask() {
            ulong mask = 0;
            foreach(Card c in this)
                mask |= c.Mask;
            return mask;
        }

        /// <summary>
        /// determines if the board contains a specific card
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        public bool Contains(Card card) {
            ulong check = code;
            int iterator = Count;
            while(--iterator > 0) {
                if((check & 63) == card.Code) return true;
                check >>= 6;
            }
            return false;
        }

        /// <summary>
        /// number of cards in the board
        /// </summary>
        public int Count {
            get { return (int)(code >> 60); }
            set {
                code &= 0xFFFFFFFFFFFFFFF;
                code |= (ulong)value << 60;
            }
        }

        /// <summary>
        /// code of the board
        /// </summary>
        public ulong Code {
            get { return code; }
        }

        /// <summary>
        /// adds a card to the board
        /// </summary>
        /// <param name="card"></param>
        public void Add(Card card) {
            code |= (ulong)card.Code << (Count * 6);
            ++Count;
        }
        
        /// <summary>
        /// adds a range of cards to the board
        /// </summary>
        /// <param name="cards"></param>
        public void AddRange(IEnumerable<Card> cards) {
            foreach(Card card in cards)
                Add(card);
        }

        /// <summary>
        /// removes multiple cards from the board
        /// </summary>
        /// <param name="count"></param>
        public void RemoveCount(int count) {
            Count -= count;
            code &= ((1U << (Count * 6)) - 1) | 0xF000000000000000;
        }

        /// <summary>
        /// get an enumerator 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Card> GetEnumerator() {
            for(int i = 0; i < Count; ++i)
                yield return new Card((byte)((code >> (i * 6)) & 63));
        }

        /// <summary>
        /// get an enumerator
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        /// compute a hashcode for this board
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return code.GetHashCode();
        }

        /// <summary>
        /// determines if the board is equal to a specified object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            return obj is Board && ((Board)obj).code == code;
        }

        /// <summary>
        /// converts the board to a string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            foreach(Card card in this)
                sb.Append(card.ToString());
            return sb.ToString();
        }

        /// <summary>
        /// parses a board from a string
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static Board Parse(string cards) {
            Board board = new Board();
            cards = cards.Replace(" ", "");
            for(int i=0;i<cards.Length-1;i+=2)
                board.Add(Card.FromString(cards.Substring(i, 2)));
            return board;            
        }

        /// <summary>
        /// adds two boards and creates another board
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Board operator +(Board lhs, Board rhs) {
            return new Board(((lhs.code | (rhs.code << (lhs.Count * 6))) & 0xFFFFFFFFFFFFFFF) | ((ulong)(lhs.Count + rhs.Count) << 60));
        }

        /// <summary>
        /// adds a card to a board
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Board operator +(Board lhs, Card rhs) {
            return new Board(((lhs.code | ((ulong)rhs.Code << (lhs.Count * 6))) & 0xFFFFFFFFFFFFFFF) | ((ulong)(lhs.Count + 1) << 60));
        }
    }
}
