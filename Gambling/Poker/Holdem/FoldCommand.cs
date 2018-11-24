using StreamRC.Gambling.Cards;
using StreamRC.Gambling.Poker.Evaluation;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Gambling.Poker.Holdem {

    /// <summary>
    /// command used to fold a hand in a <see cref="HoldemGame"/>
    /// </summary>
    public class FoldCommand : StreamCommandHandler {
        readonly HoldemModule casino;
        readonly PlayerModule playermodule;
        readonly RPGMessageModule messagemodule;
        readonly CardImageModule cardimages;

        /// <summary>
        /// creates a new <see cref="FoldCommand"/>
        /// </summary>
        /// <param name="casino">access to holdem casino</param>
        /// <param name="playermodule">access to player data</param>
        /// <param name="messagemodule">access to message sending</param>
        /// <param name="cardimages">access to images of cards</param>
        public FoldCommand(HoldemModule casino, PlayerModule playermodule, RPGMessageModule messagemodule, CardImageModule cardimages) {
            this.casino = casino;
            this.playermodule = playermodule;
            this.messagemodule = messagemodule;
            this.cardimages = cardimages;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            long userid = playermodule.GetPlayer(command.Service, command.User).UserID;
            HoldemGame game = casino.GetGame(userid);
            if (game == null) {
                SendMessage(channel, command.User, "You have no active holdem game. Use !holdem to start a new game.");
                return;
            }

            casino.RemoveGame(userid);

            RPGMessageBuilder message = messagemodule.Create().User(userid).Text(" folds the hand. ");
            HandEvaluation dealerevaluation = HandEvaluator.Evaluate(game.Board + game.DealerHand);
            HandEvaluation playerevaluation = HandEvaluator.Evaluate(game.Board + game.PlayerHand);
            if(dealerevaluation < playerevaluation || dealerevaluation.Rank < HandRank.Pair || (dealerevaluation.Rank == HandRank.Pair && dealerevaluation.HighCard < CardRank.Four)) {
                message.ShopKeeper().Text(" laughs and shows ");
                foreach(Card card in game.DealerHand)
                    message.Image(cardimages.GetCardUrl(card), $"{card} ");
                message.Text("while grabbing ").Gold(game.Pot);
            }
            else message.ShopKeeper().Text(" gladly rakes in ").Gold(game.Pot);
            message.Send();
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}