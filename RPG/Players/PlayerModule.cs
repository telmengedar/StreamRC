using System;
using System.Linq;
using System.Threading.Tasks;
using NightlyCode.Core.Conversion;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Threading;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Data;
using StreamRC.RPG.Players.Commands;
using StreamRC.Streaming.Events;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Players {

    /// <summary>
    /// module managing player data
    /// </summary>
    [Dependency(nameof(PlayerLevelModule))]
    [Dependency(nameof(UserModule))]
    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(StreamEventModule))]
    [ModuleKey("player")]
    public class PlayerModule : IInitializableModule, IRunnableModule, ICommandModule
    {
        readonly Context context;
        readonly PeriodicTimer experiencetimer = new PeriodicTimer();
        object createplayerlock = new object();

        /// <summary>
        /// creates a new <see cref="PlayerModule"/>
        /// </summary>
        /// <param name="context">module context</param>
        public PlayerModule(Context context) {
            this.context = context;
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
            return context.Database.LoadEntities<Player>().Where(p => p.UserID == playerid).Execute().FirstOrDefault();
        }

        public int GetPlayerGold(long playerid) {
            return context.Database.Load<Player>(p => p.Gold).Where(p => p.UserID == playerid).ExecuteScalar<int>();
        }

        public PlayerAscension GetPlayerAscension(long playerid) {
            return context.Database.LoadEntities<PlayerAscension>().Where(a => a.UserID == playerid).Execute().FirstOrDefault();
        }

        /// <summary>
        /// get player of an existing user
        /// </summary>
        /// <param name="service">name of service user is linked to</param>
        /// <param name="username">name of user</param>
        /// <returns>player object</returns>
        public Player GetExistingPlayer(string service, string username) {
            User user = context.GetModule<UserModule>().GetUser(service, username);
            return GetExistingPlayer(user.ID);
        }

        public Player GetPlayer(long playerid) {
            return context.Database.LoadEntities<Player>().Where(p => p.UserID == playerid).Execute().FirstOrDefault();
        }

        /// <summary>
        /// get level of player
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <returns>player level</returns>
        public int GetLevel(long playerid) {
            return context.Database.Load<Player>(p => p.Level).Where(p => p.UserID == playerid).ExecuteScalar<int>();
        }

        /// <summary>
        /// get player of a user
        /// </summary>
        /// <param name="service">service user is connected to</param>
        /// <param name="username">name of user</param>
        /// <returns>player data of user</returns>
        public Player GetPlayer(string service, string username) {
            User user = context.GetModule<UserModule>().GetUser(service, username);
            lock(createplayerlock) {
                Player player = context.Database.LoadEntities<Player>().Where(p => p.UserID == user.ID).Execute().FirstOrDefault();
                if(player != null)
                    return player;

                context.Database.Insert<Player>()
                    .Columns(p => p.UserID, p => p.Experience, p => p.Gold, p => p.Level, p => p.CurrentHP, p => p.MaximumHP, p => p.CurrentMP, p => p.MaximumMP, p => p.Strength, p => p.Dexterity, p => p.Fitness, p => p.Luck)
                    .Values(user.ID, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
                    .Execute();
                LevelUp(user.ID, 1);
                return context.Database.LoadEntities<Player>().Where(p => p.UserID == user.ID).Execute().FirstOrDefault();
            }
        }

        public void UpdateRunningStats(long playerid, int health, int mana) {
            context.Database.Update<Player>().Set(p => p.CurrentHP == health, p => p.CurrentMP == mana).Where(p => p.UserID == playerid).Execute();
        }

        public void UpdatePeeAndPoo(long playerid, int pee, int poo, int vomit) {
            context.Database.Update<Player>().Set(p => p.Pee == pee, p => p.Poo == poo, p => p.Vomit == vomit).Where(p => p.UserID == playerid).Execute();
        }

        /// <summary>
        /// updates the health of a player using a delta value
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="deltahealth">value of changed health</param>
        public void UpdateHealth(long playerid, int deltahealth) {
            Aggregate max = Aggregate.Max(Constant.Create(0), EntityField.Create<Player>(pl => pl.CurrentHP + deltahealth));
            if(context.Database.Update<Player>().Set(p => p.CurrentHP == max.Int).Where(p => p.UserID == playerid).Execute() == 0)
                return;

            System.Data.DataTable table = context.Database.Load<Player>(p => p.CurrentHP, p => p.MaximumHP).Where(p => p.UserID == playerid).Execute();
            HealthChanged?.Invoke(playerid, Converter.Convert<int>(table.Rows[0][0]), Converter.Convert<int>(table.Rows[0][1]), deltahealth);
        }

        /// <summary>
        /// updates the gold value of a player
        /// </summary>
        /// <param name="playerid">id of player</param>
        /// <param name="deltagold">value of changed gold</param>
        public void UpdateGold(long playerid, int deltagold) {
            context.Database.Update<Player>().Set(p => p.Gold == p.Gold+deltagold).Where(p => p.UserID == playerid).Execute();
        }

        public void Revive(long playerid) {
            context.Database.Update<Player>().Set(p => p.CurrentHP == p.MaximumHP).Where(p => p.UserID == playerid).Execute();
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
            LevelEntry data = context.GetModule<PlayerLevelModule>().GetLevelData(level);
            if(data == null) {
                Logger.Warning(this, $"No data found for levelup to character level {level}");
                return;
            }

            context.Database.Update<Player>()
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
            long userid = context.GetModule<UserModule>().GetUserID(service, name);
            context.Database.Update<Player>().Set(p => p.Experience == p.Experience + experience).Where(p => p.UserID == userid).Execute();
            int level = context.Database.Load<PlayerAscension>(p => p.Level).Where(p => p.UserID == userid && p.Experience >= p.NextLevel).ExecuteScalar<int>();
            if(level > 0)
                LevelUp(userid, level + 1);
        }

        /// <summary>
        /// initializes the <see cref="T:NightlyCode.Modules.IModule"/> to prepare for start
        /// </summary>
        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<Player>();
            context.Database.UpdateSchema<PlayerAscension>();
            Task.Run(() => FixStats());
        }

        void FixStats() {
            foreach(LevelEntry data in context.GetModule<PlayerLevelModule>().GetLevelEntries()) {
                context.Database.Update<Player>()
                    .Set(p => p.MaximumHP == data.Health, p => p.MaximumMP == data.Mana, p => p.Strength == data.Strength, p => p.Dexterity == data.Dexterity, p => p.Fitness == data.Fitness, p => p.Luck == data.Luck, p=>p.Intelligence==data.Intelligence)
                    .Where(p => p.Level == data.Level)
                    .Execute();
            }
        }

        void OnTimerElapsed() {
            Aggregate maxvomit = Aggregate.Max(Constant.Create(0), EntityField.Create<Player>(p => p.Vomit - 5));
            context.Database.Update<Player>().Set(u => u.Experience == u.Experience + 1.0f, u => u.Vomit == maxvomit.Int).Where(u => u.IsActive == true).Execute();

            foreach(PlayerAscension player in context.Database.LoadEntities<PlayerAscension>().Where(a => a.Experience >= a.NextLevel).Execute())
                LevelUp(player.UserID, player.Level + 1);
        }

        void OnUserLeft(UserInformation user) {
            SetActive(GetPlayer(user.Service, user.Username).UserID, false);
        }

        void SetActive(long playerid, bool isactive) {
            context.Database.Update<Player>().Set(p => p.IsActive == isactive).Where(p => p.UserID == playerid).Execute();
            PlayerStatusChanged?.Invoke(playerid, isactive);
        }

        void OnUserJoined(UserInformation user) {
            Player player = GetPlayer(user.Service, user.Username);
            SetActive(GetPlayer(user.Service, user.Username).UserID, true);
        }

        void OnChannelHosted(HostInformation host) {
            AddExperience(host.Service, host.Channel, 10.0f + Math.Max(0, host.Viewers));
        }

        void OnRaidHosted(RaidInformation raid) {
            AddExperience(raid.Service, raid.Login, 12.0f + Math.Max(0, raid.RaiderCount) * 1.3f);
        }

        void IRunnableModule.Start() {
            experiencetimer.Elapsed += OnTimerElapsed;
            context.GetModule<StreamModule>().UserJoined += OnUserJoined;
            context.GetModule<StreamModule>().UserLeft += OnUserLeft;
            context.GetModule<StreamModule>().Hosted += OnChannelHosted;
            context.GetModule<StreamModule>().Raid += OnRaidHosted;
            context.GetModule<StreamModule>().ChatMessage += OnChatMessage;
            context.GetModule<StreamEventModule>().EventValue += OnEventValue;

            context.Database.Update<Player>().Set(p => p.IsActive == false).Execute();
            experiencetimer.Start(TimeSpan.FromMinutes(1.0));

            context.GetModule<StreamModule>().RegisterCommandHandler("character", new CharacterStatsCommandHandler(this, context.GetModule<UserModule>()));
        }

        void OnEventValue(long userid, int value) {
            UpdateGold(userid, value);
        }

        void OnChatMessage(ChatMessage message) {
            AddExperience(message.Service, message.User, 3 + message.Message.Length * 0.05f);
        }

        void IRunnableModule.Stop() {
            experiencetimer.Elapsed -= OnTimerElapsed;
            context.GetModule<StreamModule>().UserJoined -= OnUserJoined;
            context.GetModule<StreamModule>().UserLeft -= OnUserLeft;
            context.GetModule<StreamModule>().Hosted -= OnChannelHosted;
            context.GetModule<StreamModule>().Raid -= OnRaidHosted;
            context.GetModule<StreamModule>().ChatMessage -= OnChatMessage;
            context.GetModule<StreamEventModule>().EventValue -= OnEventValue;
            experiencetimer.Stop();

            context.GetModule<StreamModule>().UnregisterCommandHandler("character");
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