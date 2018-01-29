using System;
using System.Windows.Media.Imaging;
using StreamRC.Streaming.Cache;

namespace StreamRC.Streaming.Stream {
    public class ChatEmote {

        /// <summary>
        /// index in message where emote starts
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// index in message where emote ends
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// id of image in <see cref="ImageCacheModule"/>
        /// </summary>
        public long ImageID { get; set; }
    }
}