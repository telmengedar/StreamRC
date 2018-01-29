namespace StreamRC.Streaming.Cache {

    /// <summary>
    /// cache entry for an image
    /// </summary>
    public class ImageCacheEntry {

        /// <summary>
        /// image data
        /// </summary>
        public ImageCacheItem Image { get; set; }

        /// <summary>
        /// time after which image is removed from cache
        /// </summary>
        public double LifeTime { get; set; }
    }
}