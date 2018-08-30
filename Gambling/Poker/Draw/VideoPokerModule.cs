using System.Collections.Generic;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Gambling.Poker.Draw {

    /// <summary>
    /// module providing a video poker game
    /// </summary>
    [Dependency(nameof(CardImageModule), SpecifierType.Type)]
    [Dependency(nameof(RPGMessageModule), SpecifierType.Type)]
    [Dependency(nameof(PlayerModule), SpecifierType.Type)]
    [Dependency(nameof(UserModule), SpecifierType.Type)]
    [Dependency(nameof(StreamModule), SpecifierType.Type)]
    public class VideoPokerModule : IRunnableModule {
        readonly Context context;
        readonly Dictionary<long, VideoPokerGame> games = new Dictionary<long, VideoPokerGame>();

        /// <summary>
        /// creates a new <see cref="VideoPokerGame"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public VideoPokerModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// creates a new <see cref="VideoPokerGame"/>
        /// </summary>
        /// <param name="service">service user is registered</param>
        /// <param name="user">name of user</param>
        /// <param name="bet">bet for game</param>
        /// <returns></returns>
        public VideoPokerGame CreateGame(string service, string user, int bet) {
            return CreateGame(context.GetModule<UserModule>().GetUserID(service, user), bet);
        }

        /// <summary>
        /// creates a new <see cref="VideoPokerGame"/>
        /// </summary>
        /// <param name="userid">is of user</param>
        /// <param name="bet">bet for game</param>
        /// <returns></returns>
        public VideoPokerGame CreateGame(long userid, int bet) {
            context.GetModule<PlayerModule>().UpdateGold(userid, -bet);
            VideoPokerGame game = games[userid] = new VideoPokerGame {
                Bet = bet,
                Deck = CardStack.Fresh(),
                Hand = new Board()
            };
            return game;
        }

        public VideoPokerGame GetGame(string service, string user) {
            long id = context.GetModule<UserModule>().GetUserID(service, user);
            VideoPokerGame game;
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