namespace StreamRC.Streaming.Users.Permissions {

    /// <summary>
    /// a single permission linked to a user
    /// </summary>
    public class UserPermission {

        /// <summary>
        /// id of user
        /// </summary>
        public long UserID { get; set; }

        /// <summary>
        /// permission available to the user
        /// </summary>
        public string Permission { get; set; }
    }
}