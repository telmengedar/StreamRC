using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Infos {

    /// <summary>
    /// info to be displayed to requesters
    /// </summary>
    public class Info {

        /// <summary>
        /// key used to identify info
        /// </summary>
        [PrimaryKey]
        public string Key { get; set; }

        /// <summary>
        /// text for key
        /// </summary>
        public string Text { get; set; }
    }
}