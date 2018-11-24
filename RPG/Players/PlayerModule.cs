using System;
using System.Linq;
using System.Threading.Tasks;
using NightlyCode.Core.Conversion;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Threading;
using NightlyCode.Database.Clients.Tables;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Scripts;
using StreamRC.RPG.Data;
using StreamRC.Streaming.Events;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Players {

    /// <summary>
    /// module managing player data
    /// </summary>
    [Module(Key = "player")]
    public class PlayerModule : ICommandModule
    {
        readonly IDatabaseModule database;
        readonly IStreamModule stream;
        readonly StreamEventModule streamevents;
        readonly UserModule users;
        readonly PlayerModule players;
        readonly PlayerLevelModule playerlevels;
        readonly PeriodicTimer experiencetimer = new PeriodicTimer();
        readonly object createplayerlock = new object();

        /// <summary>
        /// creates a new <see cref="PlayerModule"/>
        /// </summary>
        /// <param name="context">module context</param>
        public PlayerModule(IDatabaseModule database, IStreamModule stream, StreamEventModule streamevents, UserModule users, PlayerModule players, PlayerLevelModule playerlevels) {
            this.database = database;
            this.stream = stream;
            this.streamevents = streamevents;
            this.users = users;
            this.players = players;
            this.playerlevels = playerlevels;
            database.Database.UpdateSchema<Player>();
            database.Database.UpdateSchema<PlayerAscension>();
            Task.Run(() => FixStats());

            experiencetimer.Elapsed += OnTimerElapsed;
            stream.UserJoined += OnUserJoined;
            stream.UserLeft += OnUserLeft;
            stream.Hosted += OnChannelHosted;
            stream.Raid += OnRaidHosted;
            stream.ChatMessage += OnChatMessage;
            streamevents.EventValue += OnEventValue;

            database.Database.Update<Player>().Set(p => p.IsActive == false).Execute();
            experiencetimer.Start(TimeSpan.FromMinutes(1.0));
        }

        /// <summary>
        /// triggered when the health of a player has changed
        /// </summary>
        public event Action<long, int, int, int> HealthChanged;

        /// <summary>
        /// get player of an existing user
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <returns>player object</returns>
        public Player GetExistingPlayer(long playerid)
        {
            return database.Database.LoadEntities<Player>().Where(p => p.UserID == playerid).Execute().FirstOrDefault();
        }

        public int GetPlayerGold(long playerid) {
            return database.Database.Load<Player>(p => p.Gold).Where(p => p.UserID == playerid).ExecuteScalar<int>();
        }

        [Command("character")]
        public PlayerAscension GetPlayerAscension(long playerid) {
            return database.Database.LoadEntities<PlayerAscension>().Where(a => a.UserID == playerid).Execute().FirstOrDefault();
        }

        /// <summary>
        /// get player of an existing user
        /// </summary>
        /// <param name="service">name of service user is linked to</param>
        /// <param name="username">name of user</param>
        /// <returns>player object</returns>
        public Player GetExistingPlayer(string service, string username) {
            User user = users.GetUser(service, username);
            return GetExistingPlayer(user.ID);
        }

        public Player GetPlayer(long playerid) {
            return database.Database.LoadEntities<Player>().Where(p => p.UserID == playerid).Execute().FirstOrDefault();
        }

        /// <summary>
        /// get level of player
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <returns>player level</returns>
        public int GetLevel(long playerid) {
            return database.Database.Load<Player>(p => p.Level).Where(p => p.UserID == playerid).ExecuteScalar<int>();
        }

        /// <summary>
        /// get player of a user
        /// </summary>
        /// <param name="service">service user is connected to</param>
        /// <param name="username">name of user</param>
        /// <returns>player data of user</returns>
        public Player GetPlayer(string service, string username) {
            User user = users.GetUser(service, username);
            lock(createplayerlock) {
                Player player = database.Database.LoadEntities<Player>().Where(p => p.UserID == user.ID).Execute().FirstOrDefault();
                if(player != null)
                    return player;

                database.Database.Insert<Player>()
                    .Columns(p => p.UserID, p => p.Experience, p => p.Gold, p => p.Level, p => p.CurrentHP, p => p.MaximumHP, p => p.CurrentMP, p => p.MaximumMP, p => p.Strength, p => p.Dexterity, p => p.Fitness, p => p.Luck)
                    .Values(user.ID, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
                    .Execute();
                LevelUp(user.ID, 1);
                return database.Database.LoadEntities<Player>().Where(p => p.UserID == user.ID).Execute().FirstOrDefault();
            }
        }

        public void UpdateRunningStats(long playerid, int health, int mana) {
            database.Database.Update<Player>().Set(p => p.CurrentHP == health, p => p.CurrentMP == mana).Where(p => p.UserID == playerid).Execute();
        }

        public void UpdatePeeAndPoo(long playerid, int pee, int poo, int vomit) {
            database.Database.Update<Player>().Set(p => p.Pee == pee, p => p.Poo == poo, p => p.Vomit == vomit).Where(p => p.UserID == playerid).Execute();
        }

        /// <summary>
        /// updates the health of a player using a delta value
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="deltahealth">value of changed health</param>
        public void UpdateHealth(long playerid, int deltahealth) {
            if(database.Database.Update<Player>().Set(p => p.CurrentHP == DBFunction.Max(0, p.CurrentHP+deltahealth)).Where(p => p.UserID == playerid).Execute() == 0)
                return;

            DataTable table = database.Database.Load<Player>(p => p.CurrentHP, p => p.MaximumHP).Where(p => p.UserID == playerid).Execute();
            HealthChanged?.Invoke(playerid, Converter.Convert<int>(table.Rows[0][0]), Converter.Convert<int>(table.Rows[0][1]), deltahealth);
        }

        /// <summary>
        /// updates the gold value of a player
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="deltagold">value of changed gold</param>
        public void UpdateGold(long playerid, int deltagold) {
            database.Database.Update<Player>().Set(p => p.Gold == p.Gold+deltagold).Where(p => p.UserID == playerid).Execute();
        }

        public void Revive(long playerid) {
            database.Database.Update<Player>().Set(p => p.CurrentHP == p.MaximumHP).Where(p => p.UserID == playerid).Execute();
        }

        /// <summary>
        /// triggered when the status of a player has changed
        /// </summary>
        public event Action<long, bool> PlayerStatusChanged;

        /// <summary>
        /// triggered on level up
        /// </summary>
        public event Action<long> PlayerLevelUp;

        void LevelUp(long userid, int level) {
            LevelEntry data = playerlevels.GetLevelData(level);
            if(data == null) {
                Logger.Warning(this, $"No data found for levelup to character level {level}");
                return;
            }

            database.Database.Update<Player>()
                .Set(p => p.Level == level, p => p.Experience == p.Experience - data.Experience, p => p.CurrentHP == data.Health, p => p.MaximumHP == data.Health, p => p.CurrentMP == data.Mana, p => p.MaximumMP == data.Mana, p => p.Strength == data.Strength, p => p.Dexterity == data.Dexterity, p => p.Fitness == data.Fitness, p => p.Luck == data.Luck, p=>p.Intelligence==data.Intelligence)
                .Where(p => p.UserID == userid)
                .Execute();
            PlayerLevelUp?.Invoke(userid);
        }

        /// <summary>
        /// adds experience for a user
        /// </summary>
        /// <param name="service">service of user</param>
        /// <param name="name">username</param>
        /// <param name="experience">experience</param>
        public void AddExperience(string service, string name, float experience) {
            long userid = users.GetUserID(service, name);
            database.Database.Update<Player>().Set(p => p.Experience == p.Experience + experience).Where(p => p.UserID == userid).Execute();
            int level = database.Database.Load<PlayerAscension>(p => p.Level).Where(p => p.UserID == userid && p.Experience >= p.NextLevel).ExecuteScalar<int>();
            if(level > 0)
                LevelUp(userid, level + 1);
        }

        void FixStats() {
            foreach(LevelEntry data in playerlevels.GetLevelEntries()) {
                database.Database.Update<Player>()
                    .Set(p => p.MaximumHP == data.Health, p => p.MaximumMP == data.Mana, p => p.Strength == data.Strength, p => p.Dexterity == data.Dexterity, p => p.Fitness == data.Fitness, p => p.Luck == data.Luck, p=>p.Intelligence==data.Intelligence)
                    .Where(p => p.Level == data.Level)
                    .Execute();
            }
        }

        void OnTimerElapsed() {
            database.Database.Update<Player>().Set(u => u.Experience == u.Experience + 1.0f, u => u.Vomit == DBFunction.Max(0, u.Vomit-5)).Where(u => u.IsActive == true).Execute();

            foreach(PlayerAscension player in database.Database.LoadEntities<PlayerAscension>().Where(a => a.Experience >= a.NextLevel).Execute())
                LevelUp(player.UserID, player.Level + 1);
        }

        void OnUserLeft(UserInformation user) {
            SetActive(GetPlayer(user.Service, user.Username).UserID, false);
        }

        void SetActive(long playerid, bool isactive) {
            database.Database.Update<Player>().Set(p => p.IsActive == isactive).Where(p => p.UserID == playerid).Execute();
            PlayerStatusChanged?.Invoke(playerid, isactive);
        }

        void OnUserJoined(UserInformation user) {
            SetActive(GetPlayer(user.Service, user.Username).UserID, true);
        }

        void OnChannelHosted(HostInformation host) {
            AddExperience(host.Service, host.Channel, 10.0f + Math.Max(0, host.Viewers));
        }

        void OnRaidHosted(RaidInformation raid) {
            AddExperience(raid.Service, raid.Login, 12.0f + Math.Max(0, raid.RaiderCount) * 1.3f);
        }

        void OnEventValue(long userid, int value) {
            UpdateGold(userid, value);
        }

        void OnChatMessage(IChatChannel channel, ChatMessage message) {
            AddExperience(message.Service, message.User, 3 + message.Message.Length * 0.05f);
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "revive":
                    Revive(GetExistingPlayer(arguments[0], arguments[1]).UserID);
                    break;
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module");
            }
        }
    }
}