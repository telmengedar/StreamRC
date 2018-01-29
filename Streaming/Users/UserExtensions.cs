using System.Collections.Generic;
using System.Windows.Media;
using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Users {
    public static class UserExtensions {

        public static IEnumerable<MessageChunk> CreateMessageChunks(this User user)
        {
            if (!string.IsNullOrEmpty(user.Avatar))
                yield return new MessageChunk(MessageChunkType.Emoticon, user.Avatar);
            yield return new MessageChunk(MessageChunkType.Text, user.Name, Colors.White, FontWeight.Bold);
        }

        /// <summary>
        /// formats this user for use in a <see cref="Message"/>
        /// </summary>
        /// <param name="user">user to format</param>
        /// <param name="builder">builder to fill</param>
        public static MessageBuilder Format(this User user, MessageBuilder builder) {
            return builder.Image(user.Avatar).Bold().Color(user.Color).Text(user.Name).Reset();
        }
    }
}