using System;
using System.Collections.Generic;
using System.Globalization;

namespace StreamRC.Reviews {

    /// <summary>
    /// module managing review data
    /// </summary>
    [Module(Key="review")]
    public class ReviewModule : ICommandModule, IInitializableModule {
        readonly List<ReviewEntry> entries=new List<ReviewEntry>();

        public ReviewModule() {
        }

        public bool TimeoutEnabled { get; set; } = true;

        /// <summary>
        /// triggered when the review was changed
        /// </summary>
        public event Action ReviewChanged;

        /// <summary>
        /// entries in current review
        /// </summary>
        public IEnumerable<ReviewEntry> Entries => entries;
         
        /// <summary>
        /// clears current review
        /// </summary>
        public void Clear() {
            TimeoutEnabled = true;
            entries.Clear();
            ReviewChanged?.Invoke();
        }

        /// <summary>
        /// adds an entry to the current review
        /// </summary>
        /// <param name="entry">entry to add</param>
        public void AddEntry(ReviewEntry entry) {
            entries.Add(entry);
            ReviewChanged?.Invoke();
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "add":
                    if(arguments.Length > 3)
                        TimeoutEnabled = Converter.Convert<bool>(arguments[3]);

                    AddEntry(new ReviewEntry {
                        Name = arguments[0],
                        Value = int.Parse(arguments[1]),
                        Weight = double.Parse(arguments[2], CultureInfo.InvariantCulture)
                    });
                    break;
                case "clear":
                    Clear();
                    break;
                case "show":
                    if(arguments.Length>0)
                        TimeoutEnabled = Converter.Convert<bool>(arguments[0]);

                    ReviewChanged?.Invoke();
                    break;
                default:
                    throw new Exception($"Command '{command}' not supported");
            }
        }

        void IInitializableModule.Initialize() {
            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Display.Review", (sender, args) => new ReviewDisplay(this).Show());
        }
    }
}