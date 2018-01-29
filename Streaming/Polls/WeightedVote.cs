using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// vote weighted against user status
    /// </summary>
    [View("StreamRC.Streaming.Polls.Views.weightedvote.sql")]
    public class WeightedVote {

        /// <summary>
        /// name of poll
        /// </summary>
        public string Poll { get; set; }

        /// <summary>
        /// key for vote
        /// </summary>
        public string Vote { get; set; }

        /// <summary>
        /// name of user
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// option weight
        /// </summary>
        public int Status { get; set; }
    }
}