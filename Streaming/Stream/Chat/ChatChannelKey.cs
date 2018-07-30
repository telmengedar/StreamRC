namespace StreamRC.Streaming.Stream.Chat {

    /// <summary>
    /// key identifying a chat channel
    /// </summary>
    public class ChatChannelKey {
        /// <summary>
        /// creates a new <see cref="ChatChannelKey"/>
        /// </summary>
        /// <param name="service">name of service</param>
        /// <param name="name">name of channel</param>
        /// <param name="included">flags channel has to include</param>
        /// <param name="excluded">flags channel mustn't include</param>
        public ChatChannelKey(string service, string name, ChannelFlags included=ChannelFlags.None, ChannelFlags excluded=ChannelFlags.None) {
            Service = service;
            Name = name;
            Included = included;
            Excluded = excluded;
        }

        /// <summary>
        /// name of service channel belongs to
        /// </summary>
        public string Service { get; }

        /// <summary>
        /// name of channel
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// flags channel has to include
        /// </summary>
        public ChannelFlags Included { get; }

        /// <summary>
        /// flags channel mustn't include
        /// </summary>
        public ChannelFlags Excluded { get; }
    }
}