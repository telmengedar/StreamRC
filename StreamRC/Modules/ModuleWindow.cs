using System;
using System.Windows;
using NightlyCode.Modules;

namespace NightlyCode.StreamRC.Modules {

    /// <summary>
    /// base implementation for windows which act as a module
    /// </summary>
    public abstract class ModuleWindow : Window, IInitializableModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="ModuleWindow"/>
        /// </summary>
        /// <param name="context"></param>
        protected ModuleWindow(Context context) {
            this.context = context;
            
            Closing += (sender, args) => {
                Visibility = Visibility.Hidden;
                args.Cancel = true;
            };
        }


        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            context.Settings.Set(this, "x", Left);
            context.Settings.Set(this, "y", Top);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.WidthChanged)
                context.Settings.Set(this, "width", sizeInfo.NewSize.Width);
            if (sizeInfo.HeightChanged)
                context.Settings.Set(this, "height", sizeInfo.NewSize.Height);
        }

        protected virtual void LoadSettings() {
            double width = context.Settings.Get(this, "width", Width);
            double height = context.Settings.Get(this, "height", Height);
            double x = context.Settings.Get(this, "x", 0.0);
            double y = context.Settings.Get(this, "y", 0.0);
            Width = width;
            Height = height;
            Left = x;
            Top = y;
        }

        /// <summary>
        /// module context
        /// </summary>
        protected Context Context => context;

        public virtual void Initialize() {
            LoadSettings();
        }
    }
}