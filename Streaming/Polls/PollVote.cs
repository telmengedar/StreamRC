using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// vote for a poll
    /// </summary>
    public class PollVote {

        /// <summary>
        /// poll this vote is valid for
        /// </summary>
        [Index("poll")]
        public string Poll { get; set; }

        /// <summary>
        /// user who voted for the poll
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// option the user voted for
        /// </summary>
        public string Vote { get; set; } 
    }
}