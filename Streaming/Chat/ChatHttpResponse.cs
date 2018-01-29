using System;

namespace StreamRC.Streaming.Chat {
    public class ChatHttpResponse {
        public DateTime Timestamp { get; set; }
        public ChatHttpMessage[] Messages { get; set; } 
    }
}