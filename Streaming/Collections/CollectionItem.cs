using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Collections {

    /// <summary>
    /// item for a collection
    /// </summary>
    public class CollectionItem {

        /// <summary>
        /// name of collection
        /// </summary>
        [Index("collection")]
        public string Collection { get; set; }

        /// <summary>
        /// user who added the item
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// item name
        /// </summary>
        public string Item { get; set; }
    }
}