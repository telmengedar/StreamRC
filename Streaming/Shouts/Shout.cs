using System;
using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.Streaming.Shouts {

    /// <summary>
    /// video shout data
    /// </summary>
    public class Shout {

        /// <summary>
        /// term to look for in chat messages
        /// </summary>
        [PrimaryKey]
        public string Term { get; set; }

        /// <summary>
        /// id of video to play
        /// </summary>
        public string VideoId { get; set; }

        /// <summary>
        /// time to pass until this shout works again
        /// </summary>
        public TimeSpan Cooldown { get; set; }

        /// <summary>
        /// time in seconds when to start playing the video
        /// </summary>
        public double StartSeconds { get; set; }

        /// <summary>
        /// time in seconds when to stop playing the video
        /// </summary>
        public double EndSeconds { get; set; }

        /// <summary>
        /// volume of video to play
        /// </summary>
        public int Volume { get; set; }
    }
}