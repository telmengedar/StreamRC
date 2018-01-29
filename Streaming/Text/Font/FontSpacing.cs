using NightlyCode.Japi.Json;

namespace StreamRC.Streaming.Text.Font {
    public class FontSpacing {

        [JsonKey("lt")]
        public int TopLeft { get; set; }

        [JsonKey("lc")]
        public int CenterLeft { get; set; }

        [JsonKey("lb")]
        public int BottomLeft { get; set; }

        [JsonKey("rt")]
        public int TopRight { get; set; }

        [JsonKey("rc")]
        public int CenterRight { get; set; }

        [JsonKey("rb")]
        public int BottomRight { get; set; }
    }
}