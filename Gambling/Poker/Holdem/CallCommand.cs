using System;
using StreamRC.Gambling.Cards;
using StreamRC.Gambling.Poker.Evaluation;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Gambling.Poker.Holdem {
    public class CallCommand : StreamCommandHandler {
        readonly HoldemModule casino;
        readonly PlayerModule playermodule;
        readonly RPGMessageModule messagemodule;
        readonly CardImageModule cardimages;

        public CallCommand(HoldemModule casino, PlayerModule playermodule, RPGMessageModule messagemodule, CardImageModule cardimages) {
            this.casino = casino;
            this.playermodule = playermodule;
            this.messagemodule = messagemodule;
            this.cardimages = cardimages;
        }

        int GetMultiplier(HandRank rank) {
            switch(rank) {
                case HandRank.RoyalFlush:
                    return 101;
                case HandRank.StraightFlush:
                    return 21;
                case HandRank.FourOfAKind:
                    return 11;
                case HandRank.FullHouse:
                    return 4;
                case HandRank.Flush:
                    return 3;
                default:
                    return 2;
            }
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            long userid = playermodule.GetPlayer(command.Service, command.User).UserID;
            HoldemGame game = casino.GetGame(userid);
            if (game == null)
            {
                SendMessage(channel, command.User, "You have no active holdem game. Use !holdem to start a new game.");
                return;
            }

            int bet = Math.Min(playermodule.GetPlayerGold(userid), game.Bet);
            if(bet > 0) {
                playermodule.UpdateGold(userid, -bet);
                game.Pot += bet;
            }

            game.Muck.Push(game.Deck.Pop());
            game.Board += game.Deck.Pop();

            RPGMessageBuilder message = messagemodule.Create();
            if(game.Board.Count == 5) {
                // showdown
                message.Text("Showdown! ");
            }

            message.Text("The board shows ");
            foreach (Card card in game.Board)
                message.Image(cardimages.GetCardUrl(card), $"{card} ");
            message.Text(". You have ");
            foreach (Card card in game.PlayerHand)
                message.Image(cardimages.GetCardUrl(card), $"{card} ");

            HandEvaluation evaluation = HandEvaluator.Evaluate(game.Board + game.PlayerHand);
            message.Text($" ({evaluation})");

            if(game.Board.Count == 5) {
                message.Text(". ").ShopKeeper().Text(" shows ");
                foreach (Card card in game.DealerHand)
                    message.Image(cardimages.GetCardUrl(card), $"{card} ");
                HandEvaluation dealerevaluation = HandEvaluator.Evaluate(game.Board + game.DealerHand);
                message.Text($" ({dealerevaluation}). ");

                int multiplier = 0;

                if(dealerevaluation.Rank < HandRank.Pair || (dealerevaluation.Rank == HandRank.Pair && dealerevaluation.HighCard < CardRank.Four)) {
                    message.ShopKeeper().Text(" isn't qualified for a showdown.");
                    multiplier = GetMultiplier(evaluation.Rank);
                }
                else if(dealerevaluation>evaluation) {
                    message.ShopKeeper().Text(" wins the hand and ").Gold(game.Pot).Text(" laughing at your face.");
                }
                else if(dealerevaluation == evaluation) {
                    message.ShopKeeper().Text(" Has the same hand as you.");
                    multiplier = 1;
                }
                else {
                    message.Text(" You win the hand.");
                    multiplier = GetMultiplier(evaluation.Rank);
                }

                if(multiplier > 0) {
                    message.Text(" Payout is ").Gold(game.Pot * multiplier);
                    playermodule.UpdateGold(userid, game.Pot * multiplier);
                }

                casino.RemoveGame(userid);
            }

            message.Send();
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Calls a hand and puts the bet amount of the game into the pot for another card.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}