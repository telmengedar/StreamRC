namespace StreamRC.Streaming.Text.Images
{
    internal struct PixelData24
    {
        public PixelData24(byte red, byte green, byte blue)
        {
            this.blue = blue;
            this.green = green;
            this.red = red;
        }

        public byte blue;
        public byte green;
        public byte red;
    }
}
