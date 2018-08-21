using StreamRC.Gambling.Cards;
using StreamRC.Gambling.Cards.Evaluation;

namespace StreamRC.Gambling.Poker.Evaluation
{

    /// <summary>
    /// evaluation of a poker hand
    /// </summary>
    public struct HandEvaluation
    {
        public HandRank Rank;
        public CardRank HighCard;
        public CardRank LowCard;
        public CardRank[] Kickers;

        public HandEvaluation(HandRank rank, CardRank highcard, CardRank lowcard, params CardRank[] kickers)
        {
            Rank = rank;
            HighCard = highcard;
            LowCard = lowcard;
            Kickers = kickers;
        }

        public int Distance(HandEvaluation evaluation)
        {
            if (Rank == evaluation.Rank)
            {
                if (HighCard == evaluation.HighCard)
                {
                    if (LowCard == evaluation.LowCard)
                    {
                        if (Kickers.Length == evaluation.Kickers.Length)
                        {
                            for (int i = 0; i < Kickers.Length; ++i)
                            {
                                if (Kickers[i] != evaluation.Kickers[i])
                                    return (int)Kickers[i] - (int)evaluation.Kickers[i];
                            }
                            return 0;
                        }
                        else return Kickers.Length - evaluation.Kickers.Length;
                    }
                    else return (int)LowCard - (int)evaluation.LowCard;
                }
                else return (int)HighCard - (int)evaluation.HighCard;
            }
            else return (int)Rank - (int)evaluation.Rank;
        }

        public static bool operator ==(HandEvaluation lhs, HandEvaluation rhs)
        {
            return lhs.Distance(rhs) == 0;
        }

        public static bool operator !=(HandEvaluation lhs, HandEvaluation rhs)
        {
            return lhs.Distance(rhs) != 0;
        }

        public static bool operator <(HandEvaluation lhs, HandEvaluation rhs)
        {
            return lhs.Distance(rhs) < 0;
        }

        public static bool operator >(HandEvaluation lhs, HandEvaluation rhs)
        {
            return lhs.Distance(rhs) > 0;
        }

        public static bool operator <=(HandEvaluation lhs, HandEvaluation rhs)
        {
            return lhs.Distance(rhs) <= 0;
        }

        public static bool operator >=(HandEvaluation lhs, HandEvaluation rhs)
        {
            return lhs.Distance(rhs) >= 0;
        }

        public override string ToString() {
            switch(Rank) {
                case HandRank.FourOfAKind:
                    return "4 of a kind";
                case HandRank.FullHouse:
                    return "Full House";
                case HandRank.HighCard:
                    return "High Card";
                case HandRank.RoyalFlush:
                    return "Royal Flush";
                case HandRank.StraightFlush:
                    return "Straight Flush";
                case HandRank.ThreeOfAKind:
                    return "3 of a kind";
                case HandRank.TwoPair:
                    return "Two Pair";
                default:
                    return Rank.ToString();
            }
        }
    }
}
