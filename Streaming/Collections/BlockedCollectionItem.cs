namespace StreamRC.Streaming.Collections {

    /// <summary>
    /// item which is blocked for adding to a collection
    /// </summary>
    public class BlockedCollectionItem {

        /// <summary>
        /// name of collection item belongs to
        /// </summary>
        public string Collection { get; set; }

        /// <summary>
        /// name of the blocked item
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        /// reason the item is blocked
        /// </summary>
        public string Reason { get; set; } 
    }
}