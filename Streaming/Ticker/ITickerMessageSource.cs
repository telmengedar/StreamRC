
namespace StreamRC.Streaming.Ticker {

    /// <summary>
    /// interface for a message source for the <see cref="TickerModuleWindow"/>
    /// </summary>
    public interface ITickerMessageSource {

        /// <summary>
        /// generates a ticker message to be displayed
        /// </summary>
        /// <returns>ticker message content</returns>
        TickerMessage GenerateTickerMessage();
    }
}