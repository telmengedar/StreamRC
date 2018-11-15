using System;
using NightlyCode.Database.Entities.Attributes;

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
        [Unique("key")]
        public string Key { get; set; }

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