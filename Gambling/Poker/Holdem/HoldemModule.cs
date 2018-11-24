using System.Collections.Generic;
using NightlyCode.Modules;
using StreamRC.Core.Scripts;
using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Gambling.Poker.Holdem {

    /// <summary>
    /// provides a casino holdem game to the stream
    /// </summary>
    [Module]
    [ModuleCommand("holdem", typeof(HoldemCommand))]
    [ModuleCommand("call", typeof(CallCommand))]
    [ModuleCommand("fold", typeof(FoldCommand))]
    public class HoldemModule {
        readonly UserModule users;
        readonly Dictionary<long, HoldemGame> games = new Dictionary<long, HoldemGame>();

        /// <summary>
        /// creates a new <see cref="HoldemGame"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public HoldemModule(UserModule users, IStreamModule stream, PlayerModule players, RPGMessageModule messages, CardImageModule cardimages) {
            this.users = users;
        }

        /// <summary>
        /// get a game registered for a user
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">name of user</param>
        /// <returns>game registered for user</returns>
        public HoldemGame GetGame(string service, string user) {
            return GetGame(users.GetUserID(service, user));
        }

        /// <summary>
        /// get a game registered in module
        /// </summary>
        /// <param name="userid">id of user</param>
        /// <returns>game registered for user</returns>
        public HoldemGame GetGame(long userid) {
            games.TryGetValue(userid, out HoldemGame game);
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
            return CreateGame(users.GetUserID(service, user), bet);
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
            RemoveGame(users.GetUserID(service, user));
        }

        /// <summary>
        /// removes a game from game pool
        /// </summary>
        /// <param name="userid">id of user of which to remove the game</param>
        public void RemoveGame(long userid) {
            games.Remove(userid);
        }
    }
}