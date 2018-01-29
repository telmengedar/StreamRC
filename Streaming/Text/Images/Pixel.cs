namespace StreamRC.Streaming.Text.Images
{

    /// <summary>
    /// color data of a pixel
    /// </summary>
    public struct Pixel
    {

        public Pixel(byte alpha, byte red, byte green, byte blue)
        {
            Alpha = alpha;
            Red = red;
            Green = green;
            Blue = blue;
        }

        /// <summary>
        /// transparency value
        /// </summary>
        public byte Alpha;

        /// <summary>
        /// red component
        /// </summary>
        public byte Red;

        /// <summary>
        /// green component
        /// </summary>
        public byte Green;

        /// <summary>
        /// blue component
        /// </summary>
        public byte Blue;
    }
}