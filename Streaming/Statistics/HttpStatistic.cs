using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Statistics {
    public class HttpStatistic {
        public string Name { get; set; }
        public MessageChunk[] Content { get; set; } 
    }
}