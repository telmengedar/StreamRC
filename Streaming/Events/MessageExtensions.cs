using System.Drawing;
using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Events {

    /// <summary>
    /// extensions for message builders
    /// </summary>
    public static class MessageExtensions {

        /// <summary>
        /// creates a title for a messagebox
        /// </summary>
        /// <param name="builder">builder for messages</param>
        /// <param name="title">title text</param>
        /// <returns>message builder for flow based building</returns>
        public static MessageBuilder Title(this MessageBuilder builder, string title) {
            return builder.Text(title, Color.FromArgb(240, 240, 240), FontWeight.Bold);
        }

        /// <summary>
        /// creates a title for a messagebox
        /// </summary>
        /// <param name="builder">builder for messages</param>
        /// <param name="title">title text</param>
        /// <returns>message builder for flow based building</returns>
        public static MessageBuilder EventTitle(this MessageBuilder builder, string title)
        {
            return builder.Text(title, Color.FromArgb(224, 255, 224), FontWeight.Bold);
        }

        public static MessageBuilder None(this MessageBuilder builder) {
            return builder.Text("<none>", Color.LightGray);
        }

        public static MessageBuilder Score(this MessageBuilder builder, int score)
        {
            return builder.Text($" ({score})", Color.LightGoldenrodYellow);
        }
    }
}