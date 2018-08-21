using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;

namespace StreamRC.Gambling.BlackJack {

    /// <summary>
    /// logic for black jack games
    /// </summary>
    public class BlackJackLogic {

        /// <summary>
        /// get (highest possible) value of a card in black jack
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        public int GetValue(Card card) {
            switch (card.Rank)
            {
                case CardRank.Ace:
                    return 11;
                case CardRank.King:
                case CardRank.Queen:
                case CardRank.Jack:
                case CardRank.Ten:
                    return 10;
                default:
                    return (int)card.Rank + 2;
            }
        }

        public bool IsSplitPossible(Board board) {
            return board.Count == 2 && board[0].Rank == board[1].Rank;
        }

        public void CheckSplit(Board board, RPGMessageBuilder messages) {
            if (IsSplitPossible(board))
                messages.Text(" Split possible.");
        }

        /// <summary>
        /// evaluates the value of a black jack board
        /// </summary>
        /// <param name="board">board to evaluate</param>
        /// <returns>value of board</returns>
        public int Evaluate(Board board) {
            int value = 0;
            foreach(Card card in board)
                value += GetValue(card);

            if(value > 21) {
                foreach(Card card in board) {
                    if(card.Rank == CardRank.Ace) {
                        value -= 10;
                        if(value <= 21)
                            return value;
                    }
                }
            }

            return value;
        }

        public void PlayoutDealer(BlackJackGame game, RPGMessageBuilder messages, PlayerModule playermodule, CardImageModule images) {
            messages.ShopKeeper().Text(" is drawing his hand to ");

            int value = 0;
            do {
                game.DealerBoard += game.Stack.Pop();
                value = Evaluate(game.DealerBoard);
            }
            while(value < 17);

            foreach (Card card in game.DealerBoard)
                messages.Image(images.GetCardUrl(card), $"{card} ");

            if (value > 21)
                messages.Text(" Bust!");
            else messages.Text($"({value}) ");

            int payout = 0;
            if(value > 21) {
                foreach(BlackJackBoard board in game.PlayerBoards)
                    payout = board.Bet * 2;
            }
            else {
                foreach(BlackJackBoard board in game.PlayerBoards) {
                    int boardvalue = Evaluate(board.Board);
                    if(boardvalue>value)
                        payout = board.Bet * 2;
                    else if(boardvalue == value)
                        payout = board.Bet;
                }
            }

            if (payout > 0) {
                messages.Text("Payout is ").Gold(payout);
                playermodule.UpdateGold(game.PlayerID, payout);
            }
            else {
                messages.ShopKeeper().Text(" laughs at you.");
            }
        }
    }
}