using System.Collections.Generic;
using NightlyCode.Modules;
using StreamRC.Core.Scripts;
using StreamRC.Gambling.Cards;
using StreamRC.Streaming.Users;

namespace StreamRC.Gambling.BlackJack {

    /// <summary>
    /// provides black jack to the stream chat
    /// </summary>
    [Module]
    [ModuleCommand("bj", typeof(StartBlackJackGameCommand))]
    [ModuleCommand("hit", typeof(HitCardCommand))]
    [ModuleCommand("stand", typeof(StandBoardCommand))]
    [ModuleCommand("split", typeof(SplitCommand))]
    public class BlackJackModule {
        readonly UserModule users;

        readonly Dictionary<long, BlackJackGame> games=new Dictionary<long, BlackJackGame>();
         
        /// <summary>
        /// creates a new <see cref="BlackJackModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public BlackJackModule(UserModule users) {
            this.users = users;
        }

        /// <summary>
        /// get game of user
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">username</param>
        /// <returns>active game of user</returns>
        public BlackJackGame GetGame(string service, string user) {
            long userid = users.GetUserID(service, user);
            games.TryGetValue(userid, out BlackJackGame game);
            return game;
        }

        /// <summary>
        /// starts a new game of black jack for a user
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">username</param>
        /// <returns>newly created black jack game</returns>
        public BlackJackGame StartGame(string service, string user) {
            long userid = users.GetUserID(service, user);
            BlackJackGame game = new BlackJackGame {
                PlayerID = userid,
                Stack = CardStack.Fresh()
            };
            games[userid] = game;
            return game;
        }

        public void RemoveGame(string service, string user) {
            RemoveGame(users.GetUserID(service, user));
        }

        public void RemoveGame(long userid) {
            games.Remove(userid);
        }
    }
}