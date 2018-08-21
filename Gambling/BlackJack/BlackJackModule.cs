using System.Collections.Generic;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Gambling.BlackJack {

    /// <summary>
    /// provides black jack to the stream chat
    /// </summary>
    [Dependency(nameof(StreamModule), SpecifierType.Type)]
    public class BlackJackModule : IRunnableModule {
        readonly Context context;

        readonly Dictionary<long, BlackJackGame> games=new Dictionary<long, BlackJackGame>();
         
        /// <summary>
        /// creates a new <see cref="BlackJackModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public BlackJackModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// get game of user
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">username</param>
        /// <returns>active game of user</returns>
        public BlackJackGame GetGame(string service, string user) {
            long userid = context.GetModule<UserModule>().GetUserID(service, user);
            BlackJackGame game;
            games.TryGetValue(userid, out game);
            return game;
        }

        /// <summary>
        /// starts a new game of black jack for a user
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">username</param>
        /// <returns>newly created black jack game</returns>
        public BlackJackGame StartGame(string service, string user) {
            long userid = context.GetModule<UserModule>().GetUserID(service, user);
            BlackJackGame game = new BlackJackGame {
                PlayerID = userid,
                Stack = CardStack.Fresh()
            };
            games[userid] = game;
            return game;
        }

        public void RemoveGame(string service, string user) {
            RemoveGame(context.GetModule<UserModule>().GetUserID(service, user));
        }

        public void RemoveGame(long userid) {
            games.Remove(userid);
        }

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().RegisterCommandHandler("bj", new StartBlackJackGameCommand(this, context.GetModule<PlayerModule>(), context.GetModule<RPGMessageModule>(), context.GetModule<CardImageModule>()));
            context.GetModule<StreamModule>().RegisterCommandHandler("hit", new HitCardCommand(this, context.GetModule<PlayerModule>(), context.GetModule<RPGMessageModule>(), context.GetModule<CardImageModule>()));
            context.GetModule<StreamModule>().RegisterCommandHandler("stand", new StandBoardCommand(this, context.GetModule<PlayerModule>(), context.GetModule<RPGMessageModule>(), context.GetModule<CardImageModule>()));
            context.GetModule<StreamModule>().RegisterCommandHandler("split", new SplitCommand(this, context.GetModule<PlayerModule>(), context.GetModule<RPGMessageModule>(), context.GetModule<CardImageModule>()));
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().UnregisterCommandHandler("bj");
            context.GetModule<StreamModule>().UnregisterCommandHandler("hit");
            context.GetModule<StreamModule>().UnregisterCommandHandler("stand");
            context.GetModule<StreamModule>().UnregisterCommandHandler("split");
        }
    }
}