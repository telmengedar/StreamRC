using System;

namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// interface for a module providing stats for a stream
    /// </summary>
    public interface IStreamStatsModule {

        /// <summary>
        /// triggered when viewer count has changed
        /// </summary>
        event Action<int> ViewersChanged;

        /// <summary>
        /// number of viewers in the stream
        /// </summary>
        int Viewers { get; } 
    }
}