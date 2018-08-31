using NightlyCode.Modules;

namespace NightlyCode.StreamRC.Modules {

    /// <summary>
    /// accessor for module settings
    /// </summary>
    public class ModuleSettings {
        readonly IModule module;
        readonly ISettings settings;

        /// <summary>
        /// creates a new <see cref="ModuleSettings"/> accessor
        /// </summary>
        /// <param name="module"></param>
        /// <param name="settings"></param>
        public ModuleSettings(IModule module, ISettings settings) {
            this.module = module;
            this.settings = settings;
        }

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="key">key of setting to get</param>
        /// <returns>value of setting</returns>
        public T Get<T>(string key) {
            return settings.Get<T>(module, key);
        }

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="key">key of setting to get</param>
        /// <param name="defaultvalue">default value to return when setting is not found</param>
        /// <returns>value of setting or default value if setting is not found</returns>
        public T Get<T>(string key, T defaultvalue) {
            return settings.Get(module, key, defaultvalue);
        }

        /// <summary>
        /// set a setting of a module
        /// </summary>
        /// <param name="key">key of setting to set</param>
        /// <param name="value">value to set</param>
        public void Set(string key, object value) {
            settings.Set(module, key, value);
        }
    }
}