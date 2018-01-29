using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Collections {

    /// <summary>
    /// a collection of something
    /// </summary>
    public class Collection {

        /// <summary>
        /// name of collection
        /// </summary>
        [Unique]
        public string Name { get; set; }

        /// <summary>
        /// description for collection
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// items a single user can add
        /// </summary>
        public int ItemsPerUser { get; set; }
    }
}