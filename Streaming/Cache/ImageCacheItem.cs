using System;
using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Cache {

    /// <summary>
    /// image data cached in database
    /// </summary>
    public class ImageCacheItem {

        /// <summary>
        /// id of image
        /// </summary>
        [PrimaryKey]
        [AutoIncrement]
        public long ID { get; set; }

        /// <summary>
        /// key for image identification
        /// </summary>
        [Index("key")]
        public string Key { get; set; }

        /// <summary>
        /// url to get image data
        /// </summary>
        [Unique]
        public string URL { get; set; }

        /// <summary>
        /// time the item was updated
        /// </summary>
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// time when image expires
        /// </summary>
        [Index("expiration")]
        public DateTime Expiration { get; set; }

        /// <summary>
        /// image data
        /// </summary>
        public byte[] Data { get; set; }
    }
}