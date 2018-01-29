namespace StreamRC.RPG.Data {
    public class PlayerKey {
        public PlayerKey(string service, string username) {
            Service = service;
            Username = username;
        }

        public string Service { get; set; }

        public string Username { get; set; }

        protected bool Equals(PlayerKey other) {
            return string.Equals(Service, other.Service) && string.Equals(Username, other.Username);
        }

        public override bool Equals(object obj) {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != this.GetType()) return false;
            return Equals((PlayerKey)obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((Service != null ? Service.GetHashCode() : 0) * 397) ^ (Username != null ? Username.GetHashCode() : 0);
            }
        }
    }
}