using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NightlyCode.Core.Collections;

namespace StreamRC.Reviews
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class ReviewDisplay : Window
    {
        readonly ReviewModule module;
        DispatcherTimer dispatcherTimer=new DispatcherTimer();

        readonly NotificationList<ReviewItem> items=new NotificationList<ReviewItem>();

        /// <summary>
        /// creates a new <see cref="ReviewDisplay"/>
        /// </summary>
        public ReviewDisplay(ReviewModule module)
        {
            this.module = module;
            InitializeComponent();
            module.ReviewChanged += OnReviewChanged;
            grdItems.ItemsSource = items;

            dispatcherTimer.Interval = TimeSpan.FromSeconds(10.0);
            dispatcherTimer.Tick += OnTimer;
        }

        void OnTimer(object sender, EventArgs e) {
            grdItems.Visibility = Visibility.Hidden;
            dispatcherTimer.IsEnabled = false;
        }

        void OnReviewChanged() {
            items.Clear();

            double sum = module.Entries.Sum(e => e.Weight);
            double value = 0.0f;
            foreach(ReviewEntry entry in module.Entries) {
                items.Add(new ReviewItem {
                    Topic = entry.Name,
                    Value = entry.Value
                });
                value += entry.Value * entry.Weight / sum;
            }

            items.Add(new ReviewItem {
                Topic = "Result",
                Value = (int)Math.Round(value)
            });

            grdItems.Visibility=Visibility.Visible;
            dispatcherTimer.Start();
        }
    }
}
