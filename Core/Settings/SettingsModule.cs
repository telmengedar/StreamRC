using System;
using System.Linq;
using NightlyCode.Core.Conversion;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Database.Entities.Operations.Prepared;
using NightlyCode.Modules;

namespace StreamRC.Core.Settings {

    /// <summary>
    /// module managing settings
    /// </summary>
    [Module(Key = "settings")]
    public class SettingsModule : ISettings {
        readonly PreparedLoadEntitiesOperation<Setting> loadsetting;
        readonly PreparedLoadValuesOperation countsettings;
        readonly PreparedOperation insertsetting;
        readonly PreparedOperation updatesetting;

        /// <summary>
        /// creates a new <see cref="SettingsModule"/>
        /// </summary>
        /// <param name="database">access to database</param>
        public SettingsModule(DatabaseModule database) {
            database.Database.UpdateSchema<Setting>();
            loadsetting = database.Database.LoadEntities<Setting>().Where(s => s.Module == DBParameter.String && s.Key == DBParameter.String).Prepare();
            countsettings = database.Database.Load<Setting>(s => DBFunction.Count).Where(s => s.Module == DBParameter.String && s.Key == DBParameter.String).Prepare();
            insertsetting = database.Database.Insert<Setting>().Columns(s => s.Module, s => s.Key, s => s.Value).Prepare();
            updatesetting = database.Database.Update<Setting>().Set(s => s.Value == DBParameter.String).Where(s => s.Module == DBParameter.String && s.Key == DBParameter.String).Prepare();
        }

        /// <inheritdoc />
        public event Action<string, string, object> SettingChanged;

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
            Setting setting = loadsetting.Execute(module, key).FirstOrDefault();
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
            Setting setting = loadsetting.Execute(modulename, key).FirstOrDefault();
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
            if (countsettings.ExecuteScalar<long>(module, key) == 0)
                insertsetting.Execute(module, key, value?.ToString());
            else {
                string settingvalue = value?.ToString();
                updatesetting.Execute(settingvalue, module, key);
            }

            SettingChanged?.Invoke(module, key, value);
        }

        /// <summary>
        /// set a setting of a module
        /// </summary>
        /// <param name="module">module for which to set setting</param>
        /// <param name="key">key of setting to set</param>
        /// <param name="value">value to set</param>
        public void Set(object module, string key, object value) {
            string modulename = module.GetType().Name;
            Set(modulename, key, value);
        }
    }
}