using System;
using System.Windows;
using StreamRC.Core.Settings;

namespace StreamRC.Core.UI {

    /// <summary>
    /// base implementation for windows which act as a module
    /// </summary>
    public class ModuleWindow : Window {
        readonly ISettings settings;

        /// <summary>
        /// creates a new <see cref="ModuleWindow"/>
        /// </summary>
        public ModuleWindow() { }

        /// <summary>
        /// creates a new <see cref="ModuleWindow"/>
        /// </summary>
        /// <param name="settings">access to settings</param>
        protected ModuleWindow(ISettings settings, bool hideonclose=true) {
            this.settings = settings;

            if(hideonclose) {
                Closing += (sender, args) => {
                    Visibility = Visibility.Hidden;
                    args.Cancel = true;
                };
            }

            Loaded += OnLoaded;            
        }

        void OnLoaded(object sender, RoutedEventArgs e) {
            LoadSettings();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            settings.Set(this, "x", Left);
            settings.Set(this, "y", Top);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.WidthChanged)
                settings.Set(this, "width", sizeInfo.NewSize.Width);
            if (sizeInfo.HeightChanged)
                settings.Set(this, "height", sizeInfo.NewSize.Height);
        }

        protected virtual void LoadSettings() {
            double width = settings.Get(this, "width", Width);
            double height = settings.Get(this, "height", Height);
            double x = settings.Get(this, "x", 0.0);
            double y = settings.Get(this, "y", 0.0);
            Width = width;
            Height = height;
            Left = x;
            Top = y;
        }
    }
}