namespace StreamRC.Streaming.Users {

    /// <summary>
    /// status of user
    /// </summary>
    public enum UserStatus {

        /// <summary>
        /// user without any relation to channel
        /// </summary>
        Free=1,

        /// <summary>
        /// user is following channel
        /// </summary>
        Follower=2,

        /// <summary>
        /// user is subscribed to channel
        /// </summary>
        Subscriber=6,

        /// <summary>
        /// user is subscribed with a solid subscription plan
        /// </summary>
        BigSubscriber=8,

        /// <summary>
        /// user pays a pile of money for no reason
        /// </summary>
        PremiumSubscriber=12
    }
}