namespace StreamRC.Streaming.Users {

    /// <summary>
    /// cache entry for a user
    /// </summary>
    public class UserCacheEntry {

        /// <summary>
        /// creates a new <see cref="UserCacheEntry"/>
        /// </summary>
        /// <param name="user">user to manage in cache</param>
        public UserCacheEntry(User user) {
            User = user;
            LifeTime = 300.0;
        }

        /// <summary>
        /// user data
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// time user is still hold in the cache
        /// </summary>
        public double LifeTime { get; set; }
    }
}