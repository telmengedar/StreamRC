using System.Collections.Generic;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Gambling.Poker {

    /// <summary>
    /// module providing a video poker game
    /// </summary>
    public class VideoPokerModule : IRunnableModule {
        readonly Context context;
        readonly Dictionary<long, VideoPokerContext> games = new Dictionary<long, VideoPokerContext>();

        /// <summary>
        /// creates a new <see cref="VideoPokerContext"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public VideoPokerModule(Context context) {
            this.context = context;
        }

        public VideoPokerContext CreateGame(string service, string user, int bet) {
            long id = context.GetModule<UserModule>().GetUserID(service, user);
            context.GetModule<PlayerModule>().UpdateGold(id, -bet);
            VideoPokerContext game = games[id] = new VideoPokerContext {
                Bet = bet,
                Deck = CardStack.Fresh(),
                Hand = new Board()
            };
            return game;
        }

        public VideoPokerContext GetGame(string service, string user) {
            long id = context.GetModule<UserModule>().GetUserID(service, user);
            VideoPokerContext game;
            games.TryGetValue(id, out game);
            return game;
        }

        public void RemoveGame(string service, string user) {
            long id = context.GetModule<UserModule>().GetUserID(service, user);
            games.Remove(id);
        }

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().RegisterCommandHandler("draw", new VideoPokerDrawCommand(this, context.GetModule<PlayerModule>(), context.GetModule<RPGMessageModule>(), context.GetModule<CardImageModule>()));
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().UnregisterCommandHandler("draw");
        }
    }
}