using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Twitch {

    /// <summary>
    /// profile for <see cref="TwitchModule"/>
    /// </summary>
    public class TwitchProfile {

        [PrimaryKey]
        public string Account { get; set; }

        public string AccessToken { get; set; } 
    }
}