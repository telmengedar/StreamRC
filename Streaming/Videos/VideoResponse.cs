using System;

namespace StreamRC.Streaming.Videos {
    public class VideoResponse {
        public DateTime Timestamp { get; set; }

        public StreamVideo[] Videos { get; set; }
    }
}