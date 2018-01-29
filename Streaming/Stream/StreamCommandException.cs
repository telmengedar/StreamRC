using System;
using System.Runtime.Serialization;

namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// exception during execution of a stream command
    /// </summary>
    public class StreamCommandException : Exception{

        /// <summary>
        /// creates a new <see cref="StreamCommandException"/>
        /// </summary>
        /// <param name="message">exception message</param>
        /// <param name="help"></param>
        public StreamCommandException(string message, bool help = true)
            : base(message) {
            ProvideHelp = help;
        }

        /// <summary>
        /// creates a new <see cref="StreamCommandException"/>
        /// </summary>
        /// <param name="message">exception message</param>
        /// <param name="innerException">exception which led to this exception</param>
        public StreamCommandException(string message, Exception innerException)
            : base(message, innerException) {}

        protected StreamCommandException(SerializationInfo info, StreamingContext context)
            : base(info, context) {}

        /// <summary>
        /// determines whether stream handler should show a help text to the user
        /// </summary>
        public bool ProvideHelp { get; set; }
    }
}