namespace StreamRC.Streaming.Text.Font {
    public class FontDescription {
        public FontSourceType Type { get; set; }
        public string Path { get; set; }
        public float Size { get; set; }
        public float Spacing { get; set; }
        public bool Uppercase { get; set; }
        public Glyph[] Characters { get; set; } 
    }
}