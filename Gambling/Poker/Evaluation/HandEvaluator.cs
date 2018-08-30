using StreamRC.Gambling.Cards;

namespace StreamRC.Gambling.Poker.Evaluation
{

    /// <summary>
    /// evaluates a poker hand
    /// </summary>
    public class HandEvaluator
    {
        public static HandEvaluation Evaluate(Board cards)
        {
            int[] rankcount = new int[13];
            int[] suitcount = new int[4];

            int maxsuitcount = 0;
            int maxrankcount = 0;

            int[] rankindex = { -1, -1, -1 };
            int i, k;

            // count number of same rank cards and suits
            foreach (Card c in cards)
            {
                i = ++rankcount[(int)c.Rank];
                if (i > maxrankcount) 
                    ++maxrankcount;
                if (++suitcount[(int)c.Suit] > maxsuitcount) ++maxsuitcount;
            }

            // determine rank of pairs,threeofakinds and quads
            for(i = 12; i >= 0; --i) {
                if(rankcount[i] >= 4 && rankindex[2] == -1)
                    rankindex[2] = i;
                else if(rankcount[i] == 3 && rankindex[1] == -1)
                    rankindex[1] = i;
                else if(rankcount[i] == 2 && rankindex[0] == -1)
                    rankindex[0] = i; 
            }

            CardRank highrank = CardRank.Deuce;
            CardRank lowrank = CardRank.Deuce;
            CardRank[] kickers;
            CardSuit suit = CardSuit.Clubs;
            int straightcounter = 0;
            int kickercounter = 0;

            // test for royal flush / straight flush
            if (maxsuitcount >= 5)
            {
                for (i = 3; i >= 0; --i)
                {
                    if (suitcount[i] >= 5)
                    {
                        suit = (CardSuit)i;
                        break;
                    }
                }

                // check for a straight in this suit
                for (i = 12; i >= 0; --i)
                {
                    if (cards.Contains(new Card((CardRank)i, suit)))
                    {
                        if (straightcounter == 0) highrank = (CardRank)i;
                        if (++straightcounter >= 5)
                        {
                            lowrank = (CardRank)i;
                            break;
                        }
                    }
                    else straightcounter = 0;
                }

                if (straightcounter >= 5)
                    return new HandEvaluation(highrank == CardRank.Ace ? HandRank.RoyalFlush : HandRank.StraightFlush, highrank, lowrank);
            }

            // test for quads
            if (maxrankcount >= 4)
            {
                i = rankindex[2];
                for (k = 12; k >= 0; --k)
                {
                    if (k != i && rankcount[k] > 0)
                        return new HandEvaluation(HandRank.FourOfAKind, (CardRank)i, (CardRank)i, (CardRank)k);
                }
            }

            // test for full house
            if (rankindex[1]>-1&&rankindex[0]>-1)
                return new HandEvaluation(HandRank.FullHouse, (CardRank)rankindex[2], (CardRank)rankindex[1]);

            // test for flush
            if (maxsuitcount >= 5)
            {
                for (i = 3; i >= 0; --i)
                {
                    if (suitcount[i] >= 5)
                    {
                        suit = (CardSuit)i;
                        break;
                    }
                }

                // get the best 5 kickers of this suit
                kickers = new CardRank[5];
                for (i = 12; i >= 0; --i)
                {
                    if (cards.Contains(new Card((CardRank)i, suit)))
                    {
                        if (kickercounter == 0) highrank = (CardRank)i;

                        kickers[kickercounter++] = (CardRank)i;

                        if (kickercounter == 5)
                        {
                            lowrank = (CardRank)i;
                            break;
                        }
                    }

                }

                return new HandEvaluation(HandRank.Flush, highrank, lowrank, kickers);
            }

            // test for straight
            straightcounter = 0;
            for (i = 12; i >= 0; --i)
            {
                if (rankcount[i] > 0)
                {
                    if (straightcounter == 0) highrank = (CardRank)i;
                    if (++straightcounter >= 5)
                    {
                        lowrank = (CardRank)i;
                        break;
                    }
                }
                else straightcounter = 0;
            }
            if (straightcounter >= 5) return new HandEvaluation(HandRank.Straight, highrank, lowrank);

            // test for trips (or set)
            if (maxrankcount >= 3)
            {
                highrank = (CardRank)rankindex[1];

                kickers = new CardRank[2];
                for (i = 12; i >= 0; --i)
                {
                    if (i != (int)highrank)
                    {
                        kickers[kickercounter++] = (CardRank)i;
                        if (kickercounter == 2)
                            return new HandEvaluation(HandRank.ThreeOfAKind, highrank, highrank, kickers);
                    }
                }
            }

            if (maxrankcount >= 2)
            {
                highrank = lowrank = (CardRank)rankindex[0];
                for (i = rankindex[0]-1; i >= 0; --i)
                {
                    if (rankcount[i] == 2)
                    {
                        lowrank = (CardRank)i;
                        break;
                    }
                }

                if (highrank == lowrank) straightcounter = 3;
                else straightcounter = 1;

                kickers = new CardRank[straightcounter];
                for (i = 12; i >= 0; --i)
                {
                    if (i != (int)highrank && i != (int)lowrank)
                    {
                        kickers[kickercounter++] = (CardRank)i;
                        if (kickercounter == straightcounter)
                            break;
                    }
                }

                return new HandEvaluation(highrank == lowrank ? HandRank.Pair : HandRank.TwoPair, highrank, lowrank, kickers);
            }

            kickers = new CardRank[5];
            for (i = 12; i >= 0; --i)
            {
                if (rankcount[i] > 0)
                {
                    kickers[kickercounter++] = (CardRank)i;
                    if (kickercounter == 5)
                        break;
                }
            }
            return new HandEvaluation(HandRank.HighCard, CardRank.Deuce, CardRank.Deuce, kickers);
        }

    }
}
