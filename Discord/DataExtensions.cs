namespace StreamRC.Discord {
    public static class DataExtensions {
        public static string GetAvatarURL(string userid, string hash) {
            return $"https://cdn.discordapp.com/avatars/{userid}/{hash}.png?size=256";
        }
    }
}