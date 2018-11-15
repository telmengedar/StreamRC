using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.Streaming.Ads {

    /// <summary>
    /// ad to display in chat
    /// </summary>
    public class Ad {

        /// <summary>
        /// ad key
        /// </summary>
        [PrimaryKey]
        public string Key { get; set; }

        /// <summary>
        /// text to display
        /// </summary>
        public string Text { get; set; } 

        /// <summary>
        /// determines whether the ad is displayed
        /// </summary>
        public bool Active { get; set; }
    }
}