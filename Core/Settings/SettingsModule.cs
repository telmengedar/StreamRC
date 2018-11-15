using System.Linq;
using NightlyCode.Core.Conversion;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Modules;

namespace StreamRC.Core.Settings {

    /// <summary>
    /// module managing settings
    /// </summary>
    [Module(Key = "settings")]
    public class SettingsModule : ISettings {
        readonly DatabaseModule database;

        /// <summary>
        /// creates a new <see cref="SettingsModule"/>
        /// </summary>
        /// <param name="database">access to database</param>
        public SettingsModule(DatabaseModule database) {
            this.database = database;
            database.Database.UpdateSchema<Setting>();
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
        public T Get<T>(object module, string key)
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
            Setting setting = database.Database.LoadEntities<Setting>().Where(s => s.Module == module && s.Key == key).Execute().FirstOrDefault();
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
        public T Get<T>(object module, string key, T defaultvalue) {
            string modulename = module.GetType().Name;
            Setting setting = database.Database.LoadEntities<Setting>().Where(s => s.Module == modulename && s.Key == key).Execute().FirstOrDefault();
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
            if(database.Database.Load<Setting>(DBFunction.Count).Where(s => s.Module == module && s.Key == key).ExecuteScalar<long>() == 0)
                database.Database.Insert<Setting>().Columns(s => s.Module, s => s.Key, s => s.Value).Values(module, key, value?.ToString()).Execute();
            else {
                string settingvalue = value?.ToString();
                database.Database.Update<Setting>().Set(s => s.Value == settingvalue).Where(s => s.Module == module && s.Key == key).Execute();
            }
        }

        /// <summary>
        /// set a setting of a module
        /// </summary>
        /// <param name="module">module for which to set setting</param>
        /// <param name="key">key of setting to set</param>
        /// <param name="value">value to set</param>
        public void Set(object module, string key, object value) {
            string modulename = module.GetType().Name;
            if (database.Database.Load<Setting>(DBFunction.Count).Where(s => s.Module == modulename && s.Key == key).ExecuteScalar<long>() == 0)
                database.Database.Insert<Setting>().Columns(s => s.Module, s => s.Key, s => s.Value).Values(modulename, key, value?.ToString()).Execute();
            else
            {
                string settingvalue = value?.ToString();
                database.Database.Update<Setting>().Set(s => s.Value == settingvalue).Where(s => s.Module == modulename && s.Key == key).Execute();
            }
        }
    }
}