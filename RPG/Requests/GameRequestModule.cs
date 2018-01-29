using System.Linq;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Players;
using StreamRC.Streaming;
using StreamRC.Streaming.Notifications;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Requests {

    [Dependency(nameof(UserModule), DependencyType.Type)]
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(NotificationModule), DependencyType.Type)]
    [ModuleKey("request")]
    public class GameRequestModule : IInitializableModule, ICommandModule, IStreamCommandHandler, IItemCommandModule {
        readonly Context context;

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

        void RemoveGame(string platform, string game) {
            if(context.Database.Delete<GameRequest>().Where(g => g.Platform == platform && g.Game == game).Execute() > 0)
                context.GetModule<NotificationModule>().ShowNotification(new Notification {
                    Title = "Game Request removed",
                    Content = new MessageBuilder().Text(game, StreamColors.Option, FontWeight.Bold).Text(" for ").Text(platform, StreamColors.Option, FontWeight.Bold).Text(" has been removed from the queue.").BuildMessage()
                });
        }

        void RequestGame(string service, string username, string system, string[] arguments) {
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            RequestGame(user, system, arguments);
        }

        void RequestGame(User user, string system, string[] arguments) {
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

            context.GetModule<NotificationModule>().ShowNotification(new Notification {
                Title = "New Game Request",
                Content = builder.BuildMessage()
            });
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<GameRequest>();
            context.GetModule<StreamModule>().RegisterCommandHandler(this, "requests");
        }

        void IStreamCommandHandler.ProcessStreamCommand(StreamCommand command) {
            switch(command.Command) {
                case "requests":
                    DisplayRequests(command.Service, command.User);
                    break;
                default:
                    throw new StreamCommandException($"'{command.Command}' not handled by this module.");
            }
        }

        void DisplayRequests(string service, string username) {
            GameRequest[] gamerequests = context.Database.LoadEntities<GameRequest>().Execute().ToArray();
            if(gamerequests.Length == 0) {
                context.GetModule<StreamModule>().SendMessage(service, username, "No requests currently in queue.");
                return;
            }

            context.GetModule<StreamModule>().SendMessage(service, username, $"Game Requests: {string.Join(", ", gamerequests.Select(r => $"{r.Game} ({r.Platform}{(string.IsNullOrEmpty(r.Conditions) ? "" : ", " + r.Conditions)})"))}");
        }

        string IStreamCommandHandler.ProvideHelp(string command) {
            switch(command) {
                case "requests":
                    return "Displays the request queue in chat";
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module.");
            }
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
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module");
            }
        }
    }
}