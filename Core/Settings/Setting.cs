using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.Core.Settings {

    /// <summary>
    /// setting in database
    /// </summary>
    public class Setting {

        /// <summary>
        /// module name
        /// </summary>
        [Unique("key")]
        public string Module { get; set; }

        /// <summary>
        /// setting key
        /// </summary>
        [Unique("key")]
        public string Key { get; set; }

        /// <summary>
        /// setting value
        /// </summary>
        public string Value { get; set; } 
    }
}