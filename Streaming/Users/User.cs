using System.Collections.Generic;
using System.Drawing;
using NightlyCode.DB.Entities.Attributes;
using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Users {

    /// <summary>
    /// user of streaming service
    /// </summary>
    public class User {

        /// <summary>
        /// user id
        /// </summary>
        [PrimaryKey]
        public long ID { get; set; }

        /// <summary>
        /// service user is registered on
        /// </summary>
        [Unique("user")]
        public string Service { get; set; }

        /// <summary>
        /// name of user
        /// </summary>
        [Unique("user")]
        public string Name { get; set; }

        /// <summary>
        /// key used by service
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// url to user avatar
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// color of user
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// status of user
        /// </summary>
        public UserStatus Status { get; set; }

        protected bool Equals(User other) {
            return ID == other.ID;
        }

        public override bool Equals(object obj) {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != GetType()) return false;
            return Equals((User)obj);
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }
    }
}