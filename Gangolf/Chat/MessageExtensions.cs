using StreamRC.Core.Messages;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Users;

namespace NightlyCode.StreamRC.Gangolf.Chat {
    public static class MessageExtensions {

        public static bool StartsWithVocal(this string message) {
            if(string.IsNullOrEmpty(message))
                return false;

            switch (message[0]) {
                case 'A':
                case 'a':
                case 'E':
                case 'e':
                case 'I':
                case 'i':
                case 'O':
                case 'o':
                case 'U':
                case 'u':
                    return true;
            }

            return false;
        }

        public static MessageBuilder User(this MessageBuilder message, User user, ImageCacheModule imagecache) {
            message.Image(imagecache.GetImageByUrl(user.Avatar), user.Name);
            return message;
        }
    }
}