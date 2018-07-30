namespace StreamRC.Twitch {

    /// <summary>
    /// constants for twitch app
    /// </summary>
    public class TwitchConstants {

        /// <summary>
        /// key used to identify the service
        /// </summary>
        public const string ServiceKey = "Twitch";

        /// <summary>
        /// id of twitch RC app
        /// </summary>
        public const string ClientID = "jhh6rkkuubmhz31eoachd8kkyh5luo";

        /// <summary>
        /// scopes required for twitch access
        /// </summary>
        public const string RequiredScopes = "chat_login channel_check_subscription channel_editor user_blocks_edit channel_read channel_subscriptions";
    }
}