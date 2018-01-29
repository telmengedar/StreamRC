using System.Text;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Streaming.Ads;
using StreamRC.Streaming.Infos;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Games {

    /// <summary>
    /// configures the currently played game
    /// </summary>
    [Dependency(nameof(InfoModule), DependencyType.Type)]
    [Dependency(nameof(AdModule), DependencyType.Type)]
    [ModuleKey("current")]
    public class CurrentlyPlayedModule : ICommandModule, IInitializableModule {
        readonly Context context;
        CurrentlyPlayedGame game;

        /// <summary>
        /// creates a new <see cref="CurrentlyPlayedModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public CurrentlyPlayedModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// data for currently played game
        /// </summary>
        public CurrentlyPlayedGame CurrentGame => game;

        /// <summary>
        /// clears game information
        /// </summary>
        public void Clear() {
            context.GetModule<InfoModule>().RemoveInfo("current");
            context.GetModule<AdModule>().RemoveAd("current");
        }

        /// <summary>
        /// set data for game currently played
        /// </summary>
        /// <param name="gamename">name of game</param>
        /// <param name="epithet">epithet (eg. episodic title)</param>
        /// <param name="platform">platform game was released</param>
        /// <param name="year">year game was released</param>
        /// <param name="url">url pointing to game description (eg. mobygames)</param>
        public void SetCurrentlyPlayedGame(string gamename, string epithet, string platform, int year, string url) {
            StringBuilder sb = new StringBuilder(gamename);

            if(!string.IsNullOrEmpty(epithet)) {
                sb.Append(": ");
                sb.Append(epithet);
            }

            if(!string.IsNullOrEmpty(platform) || year > 0) {
                sb.Append(" (");
                if (year > 0)
                    sb.Append(year);

                if(!string.IsNullOrEmpty(platform)) {
                    if(year > 0)
                        sb.Append(", ");
                    sb.Append(platform);
                }
                sb.Append(")");
            }

            if(!string.IsNullOrEmpty(url))
                sb.Append(" -> ").Append(url);

            string text = sb.ToString();
            context.GetModule<InfoModule>().SetInfo("current", text);
            context.GetModule<AdModule>().SetAdText("current", text);
            game = new CurrentlyPlayedGame {
                Game = gamename,
                Epithet = epithet,
                System = platform,
                Year = year,
                MobyGames = url
            };
            context.Settings.Set(this, "game", JSON.WriteString(game));
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "clear":
                    Clear();
                    break;
                case "set":
                    SetCurrentlyPlayedGame(arguments[0], arguments.Length > 1 ? arguments[1] : null, arguments.Length > 2 ? arguments[2] : null, arguments.Length > 3 ? int.Parse(arguments[3]) : 0, arguments.Length > 4 ? arguments[4] : null);
                    break;
                case "epithet":
                    SetEpithet(arguments[0]);
                    break;
                default:
                    throw new StreamCommandException($"Command '{command}' not implemented");
            }
        }

        void SetEpithet(string epithet) {
            game.Epithet = epithet;
            context.Settings.Set(this, "game", JSON.WriteString(game));
        }

        void IInitializableModule.Initialize() {
            game = JSON.Read<CurrentlyPlayedGame>(context.Settings.Get<string>(this, "game", null));
        }
    }
}