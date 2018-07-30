using NightlyCode.DB.Entities.Attributes;

namespace Youtube {
    public class YoutubeAccount {

        /// <summary>
        /// account to connect to
        /// </summary>
        [PrimaryKey]
        public string Account { get; set; }

        /// <summary>
        /// oauth2 access token used to connect to youtube
        /// </summary>
        public string AccessToken { get; set; } 
    }
}