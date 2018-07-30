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
    }
}