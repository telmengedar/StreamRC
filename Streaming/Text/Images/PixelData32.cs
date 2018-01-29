namespace StreamRC.Streaming.Text.Images
{
    internal struct PixelData32
    {
        public PixelData32(byte alpha, byte red, byte green, byte blue)
        {
            this.blue = blue;
            this.green = green;
            this.red = red;
            this.alpha = alpha;
        }

        public byte blue;
        public byte green;
        public byte red;
        public byte alpha;
    }
}
