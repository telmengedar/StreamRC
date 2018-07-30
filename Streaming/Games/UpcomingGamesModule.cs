using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Collections;
using NightlyCode.Core.Logs;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Polls;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Ticker;

namespace StreamRC.Streaming.Games {

    /// <summary>
    /// module managing games to be played next
    /// </summary>
    [Dependency(nameof(TickerModule))]
    [Dependency(nameof(PollModule))]
    [Dependency(ModuleKeys.MainWindow, SpecifierType.Key)]
    [ModuleKey("upcoming")]
    public class UpcomingGamesModule : IInitializableModule, ICommandModule, ITickerMessageSource, IRunnableModule {
        readonly Context context;

        readonly object gameslock = new object();
        readonly NotificationList<Game> games=new NotificationList<Game>();

        /// <summary>
        /// creates a new <see cref="UpcomingGamesModule"/>
        /// </summary>
        /// <param name="context"></param>
        public UpcomingGamesModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// list of games currently in list
        /// </summary>
        public IEnumerable<Game> Games => games;

        /// <summary>
        /// adds a game to the list
        /// </summary>
        /// <param name="game">game to be added</param>
        public void AddGame(string game) {
            lock(gameslock) {
                games.Add(new Game {
                    Name = game
                });
            }
        }

        /// <summary>
        /// removes a game from the list
        /// </summary>
        /// <param name="game">game to be removed</param>
        public void RemoveGame(string game) {
            lock(gameslock) {
                games.RemoveWhere(g => g.Name == game);
            }
        }

        void LoadList() {
            lock(gameslock) {
                games.Clear();
                foreach(Game game in JSON.Read<Game[]>(context.Settings.Get(this, "games", "[]")))
                    AddGame(game.Name);
            }
        }

        void SaveList() {
            lock(gameslock)
                context.Settings.Set(this, "games", JSON.WriteString(games.ToArray()));
        }

        /// <summary>
        /// initializes the <see cref="T:NightlyCode.Modules.IModule"/> to prepare for start
        /// </summary>
        void IInitializableModule.Initialize() {
            LoadList();
            games.ListChanged += (sender, args) => {
                OnGamesChanged();
            };
            games.ItemChanged += (game, property) => {
                OnGamesChanged();
            };
            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Manage.Upcoming Games", (sender, args) => new UpcomingGamesWindow(this).Show());
        }

        string GetUpcomingGames() {
            WeightedVote[] votes = context.GetModule<PollModule>().GetWeightedVotes("next");
            PollDiagramData diagramdata = new PollDiagramData(votes);
            DiagramItem nextgame = diagramdata.GetItems().FirstOrDefault();
            return nextgame?.Item;
        }

        void OnGamesChanged() {
            SaveList();
            lock(gameslock)
                Logger.Info(this, "Upcoming Games changed", string.Join(", ", games.Select(g => g.Name)));
        }

        /// <summary>
        /// processes a command
        /// </summary>
        /// <param name="command">command to execute</param>
        /// <param name="arguments">command arguments (first is command itself)</param>
        void ICommandModule.ProcessCommand(string command, string[] arguments) {
            switch(command) {
                case "add":
                    AddGame(arguments[0]);
                    break;
                case "remove":
                    RemoveGame(arguments[0]);
                    break;
                case "position":
                    RepositionGame(arguments[0], int.Parse(arguments[1]));
                    break;
                default:
                    throw new StreamCommandException($"Unsupported command '{command}'");
            }
        }

        void RepositionGame(string game, int position) {
            lock(gameslock) {
                RemoveGame(game);

                games.Insert(Clamp(position, 0, games.Count), new Game {
                    Name = game
                });
                if (games.Count == 1)
                    context.GetModule<TickerModule>().AddSource(this);
            }
        }

        int Clamp(int value, int min, int max) {
            if(value < min)
                return min;
            if(value > max)
                return max;
            return value;
        }

        TickerMessage ITickerMessageSource.GenerateTickerMessage() {
            lock(gameslock) {
                if(games.Count == 0) {
                    string nextgame = GetUpcomingGames();
                    if(nextgame == null)
                        return null;

                    return new TickerMessage {
                        Content = new MessageBuilder()
                            .Text("Next game which might get played: ")
                            .Text(nextgame, StreamColors.Game, FontWeight.Bold)
                            .BuildMessage()
                    };
                }

                Game game = games[0];
                return new TickerMessage {
                    Content = new MessageBuilder()
                        .Text("Next game played: ")
                        .Text(game.Name, StreamColors.Game, FontWeight.Bold)
                        .BuildMessage()
                };
            }
        }

        void IRunnableModule.Start() {
            context.GetModule<TickerModule>().AddSource(this);
        }

        void IRunnableModule.Stop() {
            context.GetModule<TickerModule>().RemoveSource(this);
        }
    }
}