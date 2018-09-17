using System;

namespace StreamRC.Streaming.Users {

    /// <summary>
    /// flags for a user
    /// </summary>
    [Flags]
    public enum UserFlags {

        /// <summary>
        /// No flags
        /// </summary>
        None=0x00000000,

        /// <summary>
        /// user is a bot
        /// </summary>
        Bot=0x00000001,

        /// <summary>
        /// user is initialized currently
        /// </summary>
        Initializing=0x00000002,

        /// <summary>
        /// user seems to have backseating capabilities
        /// </summary>
        Brainy=0x00000004,

        /// <summary>
        /// user is obviously a racist
        /// </summary>
        Racist=0x00000008,

        /// <summary>
        /// user being able to modify statistics
        /// </summary>
        Bureaucrat=0x00000010
    }
}