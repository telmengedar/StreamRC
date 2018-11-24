using System;
using StreamRC.Gambling.Cards;
using StreamRC.Gambling.Poker.Evaluation;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Gambling.Poker.Holdem {
    public class HoldemCommand : StreamCommandHandler {
        readonly HoldemModule casino;
        readonly PlayerModule playermodule;
        readonly RPGMessageModule messagemodule;
        readonly CardImageModule imagemodule;

        /// <summary>
        /// creates a new <see cref="HoldemCommand"/>
        /// </summary>
        /// <param name="casino">access to holdem module</param>
        /// <param name="playermodule">access to player data</param>
        /// <param name="messagemodule">access to game messages</param>
        /// <param name="imagemodule">access to card images</param>
        public HoldemCommand(HoldemModule casino, PlayerModule playermodule, RPGMessageModule messagemodule, CardImageModule imagemodule) {
            this.casino = casino;
            this.playermodule = playermodule;
            this.messagemodule = messagemodule;
            this.imagemodule = imagemodule;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            long userid = playermodule.GetPlayer(command.Service, command.User).UserID;
            HoldemGame game = casino.GetGame(userid);
            if(game != null) {
                SendMessage(channel, command.User, "You are already active in a holdem game. Use !fold to fold your hand or !call to stay active in the game.");
                return;
            }

            int bet;
            if(command.Arguments.Length == 0)
                bet = 1;
            else int.TryParse(command.Arguments[0], out bet);

            if (bet <= 0)
            {
                SendMessage(channel, command.User, $"{command.Arguments[0]} is no valid bet");
                return;
            }

            int gold = playermodule.GetPlayerGold(userid);
            if (bet > 1 && bet > gold)
            {
                SendMessage(channel, command.User, "You can't bet more than you have.");
                return;
            }

            int maxbet = playermodule.GetLevel(userid) * 10;
            if (bet > maxbet)
            {
                SendMessage(channel, command.User, $"On your level you're only allowed to bet up to {maxbet} gold.");
                return;
            }

            // allow the player to play for one gold even if he has no gold
            if(gold > 0)
                playermodule.UpdateGold(userid, -bet);

            game = casino.CreateGame(userid, bet);

            game.Deck.Shuffle();

            game.PlayerHand += game.Deck.Pop();
            game.DealerHand += game.Deck.Pop();
            game.PlayerHand += game.Deck.Pop();
            game.DealerHand += game.Deck.Pop();

            game.Muck.Push(game.Deck.Pop());
            for(int i = 0; i < 3; ++i)
                game.Board += game.Deck.Pop();

            RPGMessageBuilder message = messagemodule.Create();
            message.Text("You have ");
            foreach(Card card in game.PlayerHand)
                message.Image(imagemodule.GetCardUrl(card), $"{card} ");
            message.Text(". The board shows ");
            foreach(Card card in game.Board)
                message.Image(imagemodule.GetCardUrl(card), $"{card} ");

            HandEvaluation evaluation = HandEvaluator.Evaluate(game.Board + game.PlayerHand);
            message.Text($" ({evaluation})").Send();
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}