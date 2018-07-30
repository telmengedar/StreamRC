using NightlyCode.DB.Entities.Attributes;
using StreamRC.Twitch.Chat;

namespace StreamRC.Twitch {

    /// <summary>
    /// profile for <see cref="TwitchBotModule"/>
    /// </summary>
    public class TwitchProfile {

        /// <summary>
        /// name of account
        /// </summary>
        [PrimaryKey]
        public string Account { get; set; }

        /// <summary>
        /// token used to access twitch
        /// </summary>
        public string AccessToken { get; set; } 
    }
}