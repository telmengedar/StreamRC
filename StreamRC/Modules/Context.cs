using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NightlyCode.Core.Logs;
using NightlyCode.DB.Clients;
using NightlyCode.DB.Entities;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Modules.Context;

namespace NightlyCode.StreamRC.Modules {

    /// <summary>
    /// module context
    /// </summary>
    public class Context : ModuleContext<ModuleInformation> {
        readonly IEntityManager entitymanager = new EntityManager(DBClient.CreateSQLite("twitchrc.db3"));
        readonly HashSet<string> openwindows = new HashSet<string>();
        bool isstarted;

        protected override void OnModuleAdded(IModule module) {
            base.OnModuleAdded(module);
            if(module is ISettings)
                Settings = (ISettings)module;

            if(module is Window && Attribute.IsDefined(module.GetType(), typeof(ModuleKeyAttribute)))
                ((Window)module).IsVisibleChanged += WindowVisibilityChanged;

            if(module is IMainWindow && module is Window)
                ((Window)module).Closing += (sender, args) => Stop();
        }

        void WindowVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if(IsShuttingDown||!isstarted)
                return;

            Window window = sender as Window;
            if(window == null)
                return;

            string key = GetModuleInformation((IModule)window).Key;

            if(window.Visibility == Visibility.Visible)
                openwindows.Add(key);
            else openwindows.Remove(key);

            Settings?.Set("Context", "openwindows", JSON.WriteString(openwindows.ToArray()));
        }

        public override void Start() {
            base.Start();
            if(Settings == null)
                return;

            foreach(string module in JSON.Read<string[]>(Settings.Get("Context", "openwindows", "[]"))) {
                openwindows.Add(module);
                try {
                    (GetModuleByKey<IModule>(module) as Window)?.Show();
                }
                catch(Exception e) {
                    Logger.Warning(this, "Unable to open previously opened window", e.Message);
                }
                
            }
            isstarted = true;
        }

        /// <summary>
        /// stop all managed modules
        /// </summary>
        public override void Stop() {
            IsShuttingDown = true;
            base.Stop();
        }

        /// <summary>
        /// determines whether the module manager is shutting down
        /// </summary>
        public bool IsShuttingDown { get; private set; }

        /// <summary>
        /// access to database
        /// </summary>
        public IEntityManager Database => entitymanager;

        /// <summary>
        /// access to settings
        /// </summary>
        public ISettings Settings { get; private set; }
    }
}