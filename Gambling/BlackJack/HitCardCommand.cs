using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Gambling.BlackJack {
    public class HitCardCommand : StreamCommandHandler {
        readonly BlackJackModule blackjack;
        readonly PlayerModule playermodule;
        readonly RPGMessageModule messages;
        readonly CardImageModule images;
        readonly BlackJackLogic logic=new BlackJackLogic();

        public HitCardCommand(BlackJackModule blackjack, PlayerModule playermodule, RPGMessageModule messages, CardImageModule images) {
            this.blackjack = blackjack;
            this.playermodule = playermodule;
            this.messages = messages;
            this.images = images;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            BlackJackGame game = blackjack.GetGame(command.Service, command.User);
            if (game == null) {
                SendMessage(channel, command.User, "There is active black jack game. Start another one with !bj <bet>");
                return;
            }

            game.PlayerBoards[game.ActiveBoard].Board += game.Stack.Pop();

            int value = logic.Evaluate(game.PlayerBoards[game.ActiveBoard].Board);

            RPGMessageBuilder message = messages.Create();
            message.User(game.PlayerID).Text(" has ");
            foreach (Card card in game.PlayerBoards[game.ActiveBoard].Board)
                message.Image(images.GetCardUrl(card), $"{card} ");
            message.Text(". ");

            if (value > 21) {
                message.Text("Bust!");
                game.PlayerBoards.RemoveAt(game.ActiveBoard);
            }
            else {
                message.Text($"({value}). ");
                if (value == 21) {
                    ++game.ActiveBoard;
                }
            }
            

            if (game.PlayerBoards.Count == 0) {
                message.Text(" All hands are busted.").ShopKeeper().Text(" laughs at you.");
                blackjack.RemoveGame(game.PlayerID);
            }
            else {
                if(game.ActiveBoard >= game.PlayerBoards.Count) {
                    message.Text(" All hands have been played.");
                    logic.PlayoutDealer(game, message, playermodule, images);
                    blackjack.RemoveGame(game.PlayerID);
                }
            }
            message.Send();
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}