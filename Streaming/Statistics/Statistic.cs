using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Statistics {

    /// <summary>
    /// statistic value for statistic module
    /// </summary>
    public class Statistic {

        /// <summary>
        /// name of statistic
        /// </summary>
        [Unique]
        public string Name { get; set; }

        /// <summary>
        /// value of statistic
        /// </summary>
        public long Value { get; set; }
    }
}