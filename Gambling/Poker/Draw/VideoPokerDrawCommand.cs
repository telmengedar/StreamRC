using System.Drawing;
using System.Linq;
using NightlyCode.Core.Collections;
using StreamRC.Gambling.Cards;
using StreamRC.Gambling.Poker.Evaluation;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Gambling.Poker.Draw {

    /// <summary>
    /// executes the draw command for video poker games
    /// </summary>
    public class VideoPokerDrawCommand : StreamCommandHandler {
        readonly VideoPokerModule pokermodule;
        readonly PlayerModule playermodule;
        readonly RPGMessageModule messagemodule;
        readonly CardImageModule imagemodule;

        public VideoPokerDrawCommand(VideoPokerModule pokermodule, PlayerModule playermodule, RPGMessageModule messagemodule, CardImageModule imagemodule) {
            this.pokermodule = pokermodule;
            this.playermodule = playermodule;
            this.messagemodule = messagemodule;
            this.imagemodule = imagemodule;
        }

        int GetMultiplicator(HandEvaluation evaluation, bool ismaxbet) {
            switch(evaluation.Rank) {
                case HandRank.RoyalFlush:
                    if(ismaxbet)
                        return 4001;
                    return 251;
                case HandRank.StraightFlush:
                    return 51;
                case HandRank.FourOfAKind:
                    return 26;
                case HandRank.FullHouse:
                    return 10;
                case HandRank.Flush:
                    return 7;
                case HandRank.Straight:
                    return 5;
                case HandRank.ThreeOfAKind:
                    return 4;
                case HandRank.TwoPair:
                    return 3;
                case HandRank.Pair:
                    if(evaluation.HighCard >= CardRank.Jack)
                        return 2;
                    return 0;
                default:
                    return 0;
            }
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            VideoPokerGame game = pokermodule.GetGame(command.Service, command.User);
            long userid = playermodule.GetPlayer(command.Service, command.User).UserID;
            HandEvaluation evaluation;

            if (game == null) {
                if(command.Arguments.Length == 0) {
                    SendMessage(channel, command.User, "You have to specify a bet amount");
                    return;
                }

                int bet;
                int.TryParse(command.Arguments[0], out bet);
                if(bet == 0) {
                    SendMessage(channel, command.User, $"{command.Arguments[0]} is no valid bet");
                    return;
                }

                if (bet > playermodule.GetPlayerGold(userid)) {
                    SendMessage(channel, command.User, "You can't bet more than you have.");
                    return;
                }

                int maxbet = playermodule.GetLevel(userid) * 10;
                if (bet > maxbet) {
                    SendMessage(channel, command.User, $"On your level you're only allowed to bet up to {maxbet} gold.");
                    return;
                }

                game = pokermodule.CreateGame(command.Service, command.User, bet);
                game.IsMaxBet = bet == maxbet;
                game.Deck.Shuffle();
                for(int i = 0; i < 5; ++i)
                    game.Hand += game.Deck.Pop();

                RPGMessageBuilder message = messagemodule.Create();
                message.User(userid).Text(" has ");
                foreach(Card card in game.Hand)
                    message.Image(imagemodule.GetCardUrl(card), $"{card} ");

                evaluation = HandEvaluator.Evaluate(game.Hand);
                message.Text($"({evaluation})").Send();
            }
            else {
                RPGMessageBuilder message = messagemodule.Create();

                if(command.Arguments.Length > 0) {
                    int redraw = 0;
                    foreach(string argument in command.Arguments) {
                        if(argument.All(c => char.IsDigit(c))) {
                            int index = int.Parse(argument);
                            if(index < 1 || index > 5) {
                                SendMessage(channel, command.User, $"{index} is not a valid slot to redraw");
                                return;
                            }
                            redraw |= 1 << (index - 1);
                        }
                        else if(argument.ToLower() == "all") {
                            redraw |= 31;
                        }
                        else {
                            int index = game.Hand.IndexOf(c => c.ToString().ToLower() == argument.ToLower());
                            if(index == -1) {
                                SendMessage(channel, command.User, $"{argument} points not to a card in your hand");
                                return;
                            }
                            redraw |= 1 << index;
                        }
                    }

                    int cards = 0;
                    for(int i = 0; i < 5; ++i) {
                        if((redraw & (1 << i)) != 0) {
                            game.Hand = game.Hand.ChangeCard(i, game.Deck.Pop());
                            ++cards;
                        }
                    }
                    message.User(userid).Text($" is redrawing {cards} cards. ");
                    foreach (Card card in game.Hand)
                        message.Image(imagemodule.GetCardUrl(card), $"{card} ");

                    evaluation = HandEvaluator.Evaluate(game.Hand);
                    message.Text($"({evaluation}).");
                }
                else {
                    evaluation = HandEvaluator.Evaluate(game.Hand);
                    message.User(userid).Text(" is satisfied with the hand.");
                }

                int multiplicator = GetMultiplicator(evaluation, game.IsMaxBet);
                if(multiplicator == 0)
                    message.Text(" ").ShopKeeper().Text(" laughs about that shitty hand.");
                else {
                    int payout = game.Bet * multiplicator;
                    playermodule.UpdateGold(userid, payout);
                    message.Text(" Payout is ").Gold(payout);
                }

                message.Send();
                pokermodule.RemoveGame(command.Service, command.User);
            }
        }

        Color GetColor(CardSuit suit) {
            switch(suit) {
                case CardSuit.Clubs:
                    return Color.LightGray;
                case CardSuit.Diamonds:
                    return Color.LightBlue;
                case CardSuit.Hearts:
                    return Color.OrangeRed;
                case CardSuit.Spades:
                    return Color.LawnGreen;
                default:
                    return Color.White;
            }
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            channel.SendMessage("Starts a game of video poker. Type !draw <bet> to start a game and when you get your first cards type !draw <slot> <slot> to redraw cards of the specified slot. Just type !draw to accept your hand.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}