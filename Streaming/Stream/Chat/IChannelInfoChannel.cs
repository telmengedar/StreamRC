using System;

namespace StreamRC.Streaming.Stream.Chat {

    /// <summary>
    /// provides information about a channel
    /// </summary>
    public interface IChannelInfoChannel {

        /// <summary>
        /// this channel is hosted by someone
        /// </summary>
        event Action<IChannelInfoChannel, HostInformation> Hosted;

        /// <summary>
        /// this channel is raided by someone
        /// </summary>
        event Action<IChannelInfoChannel, RaidInformation> Raid;

        /// <summary>
        /// a micropresent was received
        /// </summary>
        event Action<IChannelInfoChannel, MicroPresent> MicroPresent;
    }
}