using System.Collections.Generic;
using NightlyCode.Modules;
using StreamRC.Core.Scripts;
using StreamRC.Gambling.Cards;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Gambling.Poker.Draw {

    /// <summary>
    /// module providing a video poker game
    /// </summary>
    [Module(AutoCreate = true)]
    [ModuleCommand("draw", typeof(VideoPokerDrawCommand))]
    public class VideoPokerModule {
        readonly PlayerModule players;
        readonly RPGMessageModule messages;
        readonly CardImageModule cardimages;
        readonly UserModule users;
        readonly Dictionary<long, VideoPokerGame> games = new Dictionary<long, VideoPokerGame>();

        /// <summary>
        /// creates a new <see cref="VideoPokerGame"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public VideoPokerModule(PlayerModule players, RPGMessageModule messages, CardImageModule cardimages, UserModule users) {
            this.players = players;
            this.messages = messages;
            this.cardimages = cardimages;
            this.users = users;
        }

        /// <summary>
        /// creates a new <see cref="VideoPokerGame"/>
        /// </summary>
        /// <param name="service">service user is registered</param>
        /// <param name="user">name of user</param>
        /// <param name="bet">bet for game</param>
        /// <returns></returns>
        public VideoPokerGame CreateGame(string service, string user, int bet) {
            return CreateGame(users.GetUserID(service, user), bet);
        }

        /// <summary>
        /// creates a new <see cref="VideoPokerGame"/>
        /// </summary>
        /// <param name="userid">is of user</param>
        /// <param name="bet">bet for game</param>
        /// <returns></returns>
        public VideoPokerGame CreateGame(long userid, int bet) {
            players.UpdateGold(userid, -bet);
            VideoPokerGame game = games[userid] = new VideoPokerGame {
                Bet = bet,
                Deck = CardStack.Fresh(),
                Hand = new Board()
            };
            return game;
        }

        public VideoPokerGame GetGame(string service, string user) {
            long id = users.GetUserID(service, user);
            games.TryGetValue(id, out VideoPokerGame game);
            return game;
        }

        public void RemoveGame(string service, string user) {
            long id = users.GetUserID(service, user);
            games.Remove(id);
        }
    }
}