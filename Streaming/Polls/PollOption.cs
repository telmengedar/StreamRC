using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Polls {
    /// <summary>
    /// option for a poll
    /// </summary>
    public class PollOption {

        /// <summary>
        /// poll for which the option is valid
        /// </summary>
        [Index("poll")]
        public string Poll { get; set; }

        /// <summary>
        /// key used to specify the option
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// description of option
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// whether poll option is locked
        /// </summary>
        public bool Locked { get; set; }
    }
}