namespace StreamRC.Streaming.Users {

    /// <summary>
    /// key identifying user
    /// </summary>
    public class UserKey {

        /// <summary>
        /// creates a new <see cref="UserKey"/>
        /// </summary>
        /// <param name="service">service the user is registered on</param>
        /// <param name="username">name of user</param>
        public UserKey(string service, string username) {
            Service = service;
            Username = username;
        }

        /// <summary>
        /// service the user is registered on
        /// </summary>
        public string Service { get; }

        /// <summary>
        /// name of user
        /// </summary>
        public string Username { get; }

        protected bool Equals(UserKey other) {
            return string.Equals(Service, other.Service) && string.Equals(Username, other.Username);
        }

        public override bool Equals(object obj) {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != this.GetType()) return false;
            return Equals((UserKey)obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((Service != null ? Service.GetHashCode() : 0) * 397) ^ (Username != null ? Username.GetHashCode() : 0);
            }
        }
    }
}