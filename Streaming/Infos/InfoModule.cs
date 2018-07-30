using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Streaming.Infos.Commands;
using StreamRC.Streaming.Infos.Management;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Infos {

    /// <summary>
    /// module providing infos to users
    /// </summary>
    [ModuleKey("info")]
    [Dependency(nameof(StreamModule))]
    [Dependency(ModuleKeys.MainWindow, SpecifierType.Key)]
    public class InfoModule : IInitializableModule, IRunnableModule, ICommandModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="InfoModule"/>
        /// </summary>
        /// <param name="context">module context</param>
        public InfoModule(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<Info>();
            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Manage.Infos", (sender, args) => new InfoManagementWindow(this).Show());
        }

        public Info GetInfo(string key) {
            return context.Database.LoadEntities<Info>().Where(i => i.Key == key).Execute().FirstOrDefault();
        }

        public void ProcessCommand(string command, string[] arguments) {
            switch(command) {
                case "set":
                    SetInfo(arguments[0], arguments[1]);
                    break;
                case "remove":
                    RemoveInfo(arguments[0]);
                    break;
            }
        }

        /// <summary>
        /// get all infos from database
        /// </summary>
        /// <returns>enumeration of infos stored in database</returns>
        public IEnumerable<Info> GetInfos() {
            return context.Database.LoadEntities<Info>().Execute();
        }

        /// <summary>
        /// removes an info from database
        /// </summary>
        /// <param name="key">key of info to remove</param>
        public void RemoveInfo(string key) {
            if(context.Database.Delete<Info>().Where(i => i.Key == key).Execute() > 0)
                Logger.Info(this, $"'{key}' removed from database.");
            else Logger.Info(this, $"'{key}' not found in database.");

        }

        /// <summary>
        /// sets text for an info
        /// </summary>
        /// <param name="key">key of info</param>
        /// <param name="text">info text</param>
        public void SetInfo(string key, string text) {
            if(context.Database.Update<Info>().Set(i => i.Text == text).Where(i => i.Key == key).Execute() == 0)
                context.Database.Insert<Info>().Columns(i => i.Key, i => i.Text).Values(key, text).Execute();
            Logger.Info(this, $"'{key}' added to database.", text);
        }

        /// <summary>
        /// changes an info
        /// </summary>
        /// <param name="oldkey">key of info to change</param>
        /// <param name="newkey">new key of info</param>
        /// <param name="text">info text</param>
        public void ChangeInfo(string oldkey, string newkey, string text) {
            if (context.Database.Update<Info>().Set(i=>i.Key==newkey, i => i.Text == text).Where(i => i.Key == oldkey).Execute() == 0)
                Logger.Info(this, $"'{oldkey}' not found in database.");
            else Logger.Info(this, $"'{oldkey}' changed", $"'{newkey}' - {text}");
        }


        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().RegisterCommandHandler("info", new InfoCommand(this));
            context.GetModule<StreamModule>().RegisterCommandHandler("infos", new InfoListCommand(this));
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().UnregisterCommandHandler("info");
            context.GetModule<StreamModule>().UnregisterCommandHandler("infos");
        }
    }
}