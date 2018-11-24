using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Gambling.BlackJack {
    public class SplitCommand : StreamCommandHandler {
        readonly BlackJackModule blackjack;
        readonly PlayerModule playermodule;
        readonly RPGMessageModule messages;
        readonly CardImageModule images;
        readonly BlackJackLogic logic = new BlackJackLogic();

        public SplitCommand(BlackJackModule blackjack, PlayerModule playermodule, RPGMessageModule messages, CardImageModule images) {
            this.blackjack = blackjack;
            this.playermodule = playermodule;
            this.messages = messages;
            this.images = images;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            BlackJackGame game = blackjack.GetGame(command.Service, command.User);
            if (game == null)
            {
                SendMessage(channel, command.User, "There is no active black jack game. Start another one with !bj <bet>");
                return;
            }

            if(!logic.IsSplitPossible(game.PlayerBoards[game.ActiveBoard].Board)) {
                SendMessage(channel, command.User, "A split is not possible on the current hand.");
                return;
            }

            int bet = game.PlayerBoards[game.ActiveBoard].Bet;
            if (bet > playermodule.GetPlayerGold(game.PlayerID))
            {
                SendMessage(channel, command.User, "You don't have enough gold to split the hand.");
                return;
            }

            playermodule.UpdateGold(game.PlayerID, -bet);

            game.PlayerBoards.Add(new BlackJackBoard {
                Bet = bet,
                Board = new Board(game.PlayerBoards[game.ActiveBoard].Board[1])
            });

            game.PlayerBoards[game.ActiveBoard].Board = game.PlayerBoards[game.ActiveBoard].Board.ChangeCard(1, game.Stack.Pop());

            RPGMessageBuilder message = messages.Create();
            
            message.User(game.PlayerID).Text(" current hand is ");
            foreach (Card card in game.PlayerBoards[game.ActiveBoard].Board)
                message.Image(images.GetCardUrl(card), $"{card} ");
            int value = logic.Evaluate(game.PlayerBoards[game.ActiveBoard].Board);
            message.Text($"({value}). ");

            while (value == 21 || (game.ActiveBoard < game.PlayerBoards.Count && game.PlayerBoards[game.ActiveBoard].Board[0].Rank == CardRank.Ace)) {
                ++game.ActiveBoard;
                if(game.ActiveBoard >= game.PlayerBoards.Count) {
                    message.Text(" All hands are played.");
                    logic.PlayoutDealer(game, message, playermodule, images);
                    value = 0;
                }
                else {
                    if(game.PlayerBoards[game.ActiveBoard].Board.Count == 0)
                        game.PlayerBoards[game.ActiveBoard].Board += game.Stack.Pop();

                    message.User(game.PlayerID).Text(" next hand is ");
                    foreach(Card card in game.PlayerBoards[0].Board)
                        message.Image(images.GetCardUrl(card), $"{card} ");
                    value = logic.Evaluate(game.PlayerBoards[game.ActiveBoard].Board);
                    message.Text($"({value}). ");
                }
            }

            if(value > 0)
                logic.CheckSplit(game.PlayerBoards[game.ActiveBoard].Board, message);

            message.Send();
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}