
using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// poll entity
    /// </summary>
    public class Poll {

        /// <summary>
        /// name of the poll
        /// </summary>
        [Unique]
        public string Name { get; set; }

        /// <summary>
        /// description for the poll
        /// </summary>
        public string Description { get; set; }
    }
}