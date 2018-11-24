using System;
using System.Collections.Generic;
using NightlyCode.Modules;
using StreamRC.Core.UI;

namespace StreamRC.Reviews {

    /// <summary>
    /// module managing review data
    /// </summary>
    [Module(Key="review")]
    public class ReviewModule {
        readonly List<ReviewEntry> entries=new List<ReviewEntry>();

        public ReviewModule(IMainWindow mainwindow) {
            mainwindow.AddMenuItem("Display.Review", (sender, args) => new ReviewDisplay(this).Show());
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

        /// <summary>
        /// adds an entry to the current review
        /// </summary>
        /// <param name="name">name of review entry</param>
        /// <param name="value">value of review entry</param>
        /// <param name="weight">weight of review entry</param>
        public void AddEntry(string name, int value, double weight) {
            AddEntry(new ReviewEntry {
                Name = name,
                Value = value,
                Weight = weight
            });
        }

        /// <summary>
        /// shows the reviews
        /// </summary>
        /// <param name="timeout">determines whether to fadeout the reviews after a time</param>
        public void Show(bool timeout = true) {
            TimeoutEnabled = timeout;
            ReviewChanged?.Invoke();
        }
    }
}