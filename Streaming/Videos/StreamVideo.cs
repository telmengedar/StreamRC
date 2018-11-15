using System;

namespace StreamRC.Streaming.Videos {

    /// <summary>
    /// a video to be played back in overlay
    /// </summary>
    public class StreamVideo {

        /// <summary>
        /// time video was added to service
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// id of video to play (youtube id)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// when to begin playing the video
        /// </summary>
        public double StartSeconds { get; set; }

        /// <summary>
        /// when to stop playing the video
        /// </summary>
        public double EndSeconds { get; set; }

        /// <summary>
        /// playback volume
        /// </summary>
        public int Volume { get; set; }
    }
}