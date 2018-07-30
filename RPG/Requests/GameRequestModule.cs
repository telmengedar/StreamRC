using System.Linq;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Players;
using StreamRC.RPG.Requests.Command;
using StreamRC.Streaming;
using StreamRC.Streaming.Notifications;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Requests {

    [Dependency(nameof(UserModule))]
    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(NotificationModule))]
    [ModuleKey("request")]
    public class GameRequestModule : IInitializableModule, IRunnableModule, ICommandModule, IItemCommandModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="GameRequestModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public GameRequestModule(Context context) {
            this.context = context;
        }

        public int GetNumberOfRequests() {
            return context.Database.Load<GameRequest>(DBFunction.Count).ExecuteScalar<int>();
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "addgame":
                    RequestGame(arguments[0], arguments[1], arguments[2], arguments.Skip(3).ToArray());
                    break;
                case "removegame":
                    RemoveGame(arguments[0], arguments[1]);
                    break;
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module");
            }
        }

        public GameRequest[] GetRequests() {
            return context.Database.LoadEntities<GameRequest>().Execute().ToArray();
        }

        public void RemoveGame(int index) {
            GameRequest[] requests = context.Database.LoadEntities<GameRequest>().Execute().ToArray();
            RemoveGame(requests[index].Platform, requests[index].Game);
        }

        public void RemoveGame(string platform, string game) {
            if(context.Database.Delete<GameRequest>().Where(g => g.Platform == platform && g.Game == game).Execute() > 0)
                context.GetModule<NotificationModule>().ShowNotification(
                    new MessageBuilder().Text("Game Request Removed").BuildMessage(),
                    new MessageBuilder().Text(game, StreamColors.Option, FontWeight.Bold).Text(" for ").Text(platform, StreamColors.Option, FontWeight.Bold).Text(" has been removed from the queue.").BuildMessage()
                );
        }

        public void RequestWildCard(string service, string username) {
            RequestGame(service, username, "???", "???");
        }

        public void RequestGame(string service, string username, string system, string game)
        {
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            RequestGame(user, system, game);
        }

        public void RemoveWildcard() {
            RemoveGame("???", "???");
        }

        void RequestGame(string service, string username, string system, params string[] arguments) {
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            RequestGame(user, system, arguments);
        }

        void RequestGame(User user, string system, params string[] arguments) {
            string game = string.Join(" ", arguments.Where(a => !a.StartsWith("#")));
            string target = string.Join(", ", arguments.Where(a => a.StartsWith("#")));

            if(string.IsNullOrEmpty(game))
                throw new StreamCommandException("You have to provide the name of the game to be played");

            context.Database.Insert<GameRequest>().Columns(g => g.UserID, g => g.Platform, g => g.Game, g => g.Conditions).Values(user.ID, system, game, target).Execute();

            MessageBuilder builder=new MessageBuilder();
            builder.Text(game, StreamColors.Option, FontWeight.Bold);
            if(!string.IsNullOrEmpty(target))
                builder.Text($" ({target})");
            builder.Text(" for ").Text(system, StreamColors.Option, FontWeight.Bold).Text(" has been requested by ").Text(user.Name, user.Color).Text(".");

            context.GetModule<NotificationModule>().ShowNotification(
                new MessageBuilder().Text("New Game Request").BuildMessage(),
                builder.BuildMessage()
                );
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<GameRequest>();
        }

        void IItemCommandModule.ExecuteItemCommand(User user, Player player, string command, params string[] arguments) {
            switch (command)
            {
                case "addgame":
                    if(arguments.Length < 1)
                        throw new ItemUseException("You have to provide the system on which the game shall be played.");
                    if(arguments.Length < 2)
                        throw new ItemUseException("Missing game name. You have to provide the name of the game to play.");
                    RequestGame(user, arguments[0], arguments.Skip(1).ToArray());
                    break;
                case "addwildcard":
                    RequestGame(user, "???", "???");
                    break;
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module");
            }
        }

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().RegisterCommandHandler("requests", new RequestListCommandHandler(this));
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().UnregisterCommandHandler("requests");
        }
    }
}