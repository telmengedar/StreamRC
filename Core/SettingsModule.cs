using System;
using System.Linq;
using NightlyCode.Core.Conversion;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Core {

    /// <summary>
    /// module managing settings
    /// </summary>
    [ModuleKey("settings")]
    public class SettingsModule : IInitializableModule, ICommandModule, ISettings {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="SettingsModule"/>
        /// </summary>
        /// <param name="context">module context</param>
        public SettingsModule(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<Setting>();
        }

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="module">module name</param>
        /// <param name="key">key of setting to get</param>
        /// <returns>value of setting</returns>
        public T Get<T>(string module, string key) {
            return Get(module, key, default(T));
        }

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="module">module for which to get setting</param>
        /// <param name="key">key of setting to get</param>
        /// <returns>value of setting</returns>
        public T Get<T>(IModule module, string key)
        {
            return Get(module, key, default(T));
        }

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="module">module for which to get setting</param>
        /// <param name="key">key of setting to get</param>
        /// <param name="defaultvalue">default value to return when setting is not found</param>
        /// <returns>value of setting or default value if setting is not found</returns>
        public T Get<T>(string module, string key, T defaultvalue) {
            Setting setting = context.Database.LoadEntities<Setting>().Where(s => s.Module == module && s.Key == key).Execute().FirstOrDefault();
            if(setting == null)
                return defaultvalue;
            return Converter.Convert<T>(setting.Value);
        }

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="module">module for which to get setting</param>
        /// <param name="key">key of setting to get</param>
        /// <param name="defaultvalue">default value to return when setting is not found</param>
        /// <returns>value of setting or default value if setting is not found</returns>
        public T Get<T>(IModule module, string key, T defaultvalue) {
            string modulename = module.GetType().Name;
            Setting setting = context.Database.LoadEntities<Setting>().Where(s => s.Module == modulename && s.Key == key).Execute().FirstOrDefault();
            if (setting == null)
                return defaultvalue;
            return Converter.Convert<T>(setting.Value);
        }

        /// <summary>
        /// set a setting of a module
        /// </summary>
        /// <param name="module">module for which to set setting</param>
        /// <param name="key">key of setting to set</param>
        /// <param name="value">value to set</param>
        public void Set(string module, string key, object value) {
            if(context.Database.Load<Setting>(DBFunction.Count).Where(s => s.Module == module && s.Key == key).ExecuteScalar<long>() == 0)
                context.Database.Insert<Setting>().Columns(s => s.Module, s => s.Key, s => s.Value).Values(module, key, value?.ToString()).Execute();
            else {
                string settingvalue = value?.ToString();
                context.Database.Update<Setting>().Set(s => s.Value == settingvalue).Where(s => s.Module == module && s.Key == key).Execute();
            }
        }

        /// <summary>
        /// set a setting of a module
        /// </summary>
        /// <param name="module">module for which to set setting</param>
        /// <param name="key">key of setting to set</param>
        /// <param name="value">value to set</param>
        public void Set(IModule module, string key, object value) {
            string modulename = module.GetType().Name;
            if (context.Database.Load<Setting>(DBFunction.Count).Where(s => s.Module == modulename && s.Key == key).ExecuteScalar<long>() == 0)
                context.Database.Insert<Setting>().Columns(s => s.Module, s => s.Key, s => s.Value).Values(modulename, key, value?.ToString()).Execute();
            else
            {
                string settingvalue = value?.ToString();
                context.Database.Update<Setting>().Set(s => s.Value == settingvalue).Where(s => s.Module == modulename && s.Key == key).Execute();
            }
        }

        /// <summary>
        /// processes a command
        /// </summary>
        /// <param name="command">command to execute</param>
        /// <param name="arguments">command arguments (first is command itself)</param>
        void ICommandModule.ProcessCommand(string command, string[] arguments) {
            switch(command) {
                case "set":
                    string module = arguments[0];
                    string key = arguments[1];
                    string value = arguments[2];
                    Set(module, key, value);
                    break;
                default:
                    throw new Exception($"Unknown command '{command}' not supported");
            }
        }
    }
}