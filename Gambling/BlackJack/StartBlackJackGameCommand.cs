using System.Collections.Generic;
using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Gambling.BlackJack {

    /// <summary>
    /// command handler used to start a new black jack game
    /// </summary>
    public class StartBlackJackGameCommand : StreamCommandHandler {
        readonly BlackJackModule blackjack;
        readonly PlayerModule playermodule;
        readonly RPGMessageModule messages;
        readonly CardImageModule images;

        readonly BlackJackLogic logic=new BlackJackLogic();

        /// <summary>
        /// creates a new <see cref="StartBlackJackGameCommand"/>
        /// </summary>
        /// <param name="blackjack">module handling black jack games</param>
        public StartBlackJackGameCommand(BlackJackModule blackjack, PlayerModule playermodule, RPGMessageModule messages, CardImageModule images) {
            this.blackjack = blackjack;
            this.playermodule = playermodule;
            this.messages = messages;
            this.images = images;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            BlackJackGame existing = blackjack.GetGame(command.Service, command.User);
            if(existing != null) {
                SendMessage(channel, command.User, "There is already a game running for you.");
                return;
            }

            if(command.Arguments.Length == 0) {
                SendMessage(channel, command.User, "You have to specify a bet amount");
                return;
            }

            long userid = playermodule.GetPlayer(command.Service, command.User).UserID;

            int.TryParse(command.Arguments[0], out int bet);
            if (bet <= 0)
            {
                SendMessage(channel, command.User, $"{command.Arguments[0]} is no valid bet");
                return;
            }

            if (bet > playermodule.GetPlayerGold(userid))
            {
                SendMessage(channel, command.User, "You can't bet more than you have.");
                return;
            }

            int maxbet = playermodule.GetLevel(userid) * 40;
            if (bet > maxbet)
            {
                SendMessage(channel, command.User, $"On your level you're only allowed to bet up to {maxbet} gold.");
                return;
            }

            BlackJackGame game = blackjack.StartGame(command.Service, command.User);
            game.Stack.Shuffle();

            game.DealerBoard += game.Stack.Pop();

            game.PlayerBoards = new List<BlackJackBoard> {
                new BlackJackBoard {Bet = bet}
            };

            game.PlayerBoards[0].Board += game.Stack.Pop();
            game.PlayerBoards[0].Board += game.Stack.Pop();

            playermodule.UpdateGold(game.PlayerID, -bet);

            RPGMessageBuilder message = messages.Create();
            message.User(userid).Text(" has ");
            foreach(Card card in game.PlayerBoards[0].Board)
                message.Image(images.GetCardUrl(card), $"{card} ");
            message.Text(".");

            int value = logic.Evaluate(game.PlayerBoards[0].Board);
            if(value == 21) {
                message.Text("Black Jack!");
                int winnings = (int)(game.PlayerBoards[0].Bet * 2.5);
                message.Text("Winnings: ").Gold(winnings);
                playermodule.UpdateGold(userid, winnings);
                blackjack.RemoveGame(userid);
            }
            else {
                message.Text($"({value}). ");
                message.ShopKeeper().Text(" shows ");
                foreach (Card card in game.DealerBoard)
                    message.Image(images.GetCardUrl(card), $"{card} ");
                message.Text($"({logic.Evaluate(game.DealerBoard)}). ");

                logic.CheckSplit(game.PlayerBoards[0].Board, message);
            }

            message.Send();
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}