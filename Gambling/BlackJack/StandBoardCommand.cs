using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Gambling.BlackJack {
    public class StandBoardCommand : StreamCommandHandler {
        readonly BlackJackModule blackjack;
        readonly PlayerModule playermodule;
        readonly RPGMessageModule messages;
        readonly CardImageModule images;
        readonly BlackJackLogic logic = new BlackJackLogic();

        public StandBoardCommand(BlackJackModule blackjack, PlayerModule playermodule, RPGMessageModule messages, CardImageModule images) {
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

            ++game.ActiveBoard;

            RPGMessageBuilder message = messages.Create();
            message.User(game.PlayerID).Text(" is satisfied.");
            if (game.ActiveBoard >= game.PlayerBoards.Count)
            {
                message.Text(" All hands have been played.");
                logic.PlayoutDealer(game, message, playermodule, images);
                blackjack.RemoveGame(game.PlayerID);
            }
            else {
                if(game.PlayerBoards[game.ActiveBoard].Board.Count == 1)
                    game.PlayerBoards[game.ActiveBoard].Board += game.Stack.Pop();

                message.Text(" Next hand is ");
                foreach (Card card in game.PlayerBoards[game.ActiveBoard].Board)
                    message.Image(images.GetCardUrl(card), $"{card} ");

                int value = logic.Evaluate(game.PlayerBoards[game.ActiveBoard].Board);
                message.Text($"({value}). ");
                logic.CheckSplit(game.PlayerBoards[game.ActiveBoard].Board, message);
            }
            message.Send();
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}