using System;

namespace StreamRC.Core.Settings {

    /// <summary>
    /// interface for an application settings accessor
    /// </summary>
    public interface ISettings {

        /// <summary>
        /// triggered when a setting was changed
        /// </summary>
        event Action<string, string, object> SettingChanged;

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="module">module name</param>
        /// <param name="key">key of setting to get</param>
        /// <returns>value of setting</returns>
        T Get<T>(string module, string key);

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="module">module for which to get setting</param>
        /// <param name="key">key of setting to get</param>
        /// <returns>value of setting</returns>
        T Get<T>(object module, string key);

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="module">module for which to get setting</param>
        /// <param name="key">key of setting to get</param>
        /// <param name="defaultvalue">default value to return when setting is not found</param>
        /// <returns>value of setting or default value if setting is not found</returns>
        T Get<T>(string module, string key, T defaultvalue);

        /// <summary>
        /// get a setting of a module
        /// </summary>
        /// <typeparam name="T">type of value to get</typeparam>
        /// <param name="module">module for which to get setting</param>
        /// <param name="key">key of setting to get</param>
        /// <param name="defaultvalue">default value to return when setting is not found</param>
        /// <returns>value of setting or default value if setting is not found</returns>
        T Get<T>(object module, string key, T defaultvalue);

        /// <summary>
        /// set a setting of a module
        /// </summary>
        /// <param name="module">module for which to set setting</param>
        /// <param name="key">key of setting to set</param>
        /// <param name="value">value to set</param>
        void Set(string module, string key, object value);

        /// <summary>
        /// set a setting of a module
        /// </summary>
        /// <param name="module">module for which to set setting</param>
        /// <param name="key">key of setting to set</param>
        /// <param name="value">value to set</param>
        void Set(object module, string key, object value);
    }
}