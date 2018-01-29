using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// poll which has some active votes
    /// </summary>
    [View("StreamRC.Streaming.Polls.Views.activepoll.sql")]
    public class ActivePoll {

        /// <summary>
        /// name of poll
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// number of votes in poll
        /// </summary>
        public int Votes { get; set; }
    }
}