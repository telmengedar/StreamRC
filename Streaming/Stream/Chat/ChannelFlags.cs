using System;

namespace StreamRC.Streaming.Stream.Chat {

    /// <summary>
    /// flags which define the nature of a channel
    /// </summary>
    [Flags]
    public enum ChannelFlags {

        /// <summary>
        /// nothing specific about the channel
        /// </summary>
        None=0,

        /// <summary>
        /// channel is controlled by a bot
        /// </summary>
        Bot=1,

        /// <summary>
        /// channel is for chat games
        /// </summary>
        Game=2,

        /// <summary>
        /// chat is to be grabbed
        /// </summary>
        Chat = 4,

        /// <summary>
        /// channel can get filled with notifications
        /// </summary>
        Notification=8,

        /// <summary>
        /// the major connection of a service
        /// </summary>
        Major=16,

        /// <summary>
        /// a command channel which content is not displayed but still parsed
        /// </summary>
        Command=32,

        /// <summary>
        /// channel display supports linebreaks
        /// </summary>
        LineBreaks=64,

        /// <summary>
        /// channel is used to send user messages
        /// </summary>
        UserChat=128,

        /// <summary>
        /// all flags set
        /// </summary>
        All=0x7FFFFFFF
    }
}