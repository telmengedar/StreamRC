using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// information about a subscriber
    /// </summary>
    public class SubscriberInformation : UserInformation {

        /// <summary>
        /// corresponding status type (subscription plan evaluated)
        /// </summary>
        public UserStatus Status { get; set; }

        public string PlanName { get; set; }
    }
}