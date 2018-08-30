using System.Collections.Generic;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Gambling.Poker.Holdem {

    /// <summary>
    /// 
    /// </summary>
    [Dependency(nameof(CardImageModule), SpecifierType.Type)]
    [Dependency(nameof(RPGMessageModule), SpecifierType.Type)]
    [Dependency(nameof(PlayerModule), SpecifierType.Type)]
    [Dependency(nameof(UserModule), SpecifierType.Type)]
    [Dependency(nameof(StreamModule), SpecifierType.Type)]
    public class HoldemModule : IRunnableModule {
        readonly Context context;
        readonly Dictionary<long, HoldemGame> games = new Dictionary<long, HoldemGame>();

        /// <summary>
        /// creates a new <see cref="HoldemGame"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public HoldemModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// get a game registered for a user
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">name of user</param>
        /// <returns>game registered for user</returns>
        public HoldemGame GetGame(string service, string user) {
            return GetGame(context.GetModule<UserModule>().GetUserID(service, user));
        }

        /// <summary>
        /// get a game registered in module
        /// </summary>
        /// <param name="userid">id of user</param>
        /// <returns>game registered for user</returns>
        public HoldemGame GetGame(long userid) {
            HoldemGame game;
            games.TryGetValue(userid, out game);
            return game;
        }

        /// <summary>
        /// creates a new hold em game
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">name of user</param>
        /// <param name="bet">bet amount for game actions</param>
        /// <returns>new holdem game</returns>
        public HoldemGame CreateGame(string service, string user, int bet) {
            return CreateGame(context.GetModule<UserModule>().GetUserID(service, user), bet);
        }

        /// <summary>
        /// creates a new hold em poker game
        /// </summary>
        /// <param name="userid">id of system user</param>
        /// <returns>new holdem game</returns>
        public HoldemGame CreateGame(long userid, int bet) {
            HoldemGame game = games[userid] = new HoldemGame
            {
                PlayerID = userid,
                Bet = bet,
                Pot = bet,
                Deck = CardStack.Fresh(),
                Muck = new CardStack(),
                Board = new Board(),
                PlayerHand = new Board(),
                DealerHand = new Board()
            };

            return game;
        }

        /// <summary>
        /// removes a game from game pool
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">name of user</param>
        public void RemoveGame(string service, string user) {
            RemoveGame(context.GetModule<UserModule>().GetUserID(service, user));
        }

        /// <summary>
        /// removes a game from game pool
        /// </summary>
        /// <param name="userid">id of user of which to remove the game</param>
        public void RemoveGame(long userid) {
            games.Remove(userid);
        }

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().RegisterCommandHandler("holdem", new HoldemCommand(this, context.GetModule<PlayerModule>(), context.GetModule<RPGMessageModule>(), context.GetModule<CardImageModule>()));
            context.GetModule<StreamModule>().RegisterCommandHandler("call", new CallCommand(this, context.GetModule<PlayerModule>(), context.GetModule<RPGMessageModule>(), context.GetModule<CardImageModule>()));
            context.GetModule<StreamModule>().RegisterCommandHandler("fold", new FoldCommand(this, context.GetModule<PlayerModule>(), context.GetModule<RPGMessageModule>(), context.GetModule<CardImageModule>()));
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().UnregisterCommandHandler("holdem");
            context.GetModule<StreamModule>().UnregisterCommandHandler("call");
            context.GetModule<StreamModule>().UnregisterCommandHandler("fold");
        }
    }
}