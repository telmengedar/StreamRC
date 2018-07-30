using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using NightlyCode.Core.Conversion;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Stream {
    public static class MessageExtensions {

        static System.Drawing.Color ColorOrDefault(string color) {
            try {
                if(string.IsNullOrEmpty(color))
                    return System.Drawing.Color.White;

                return Converter.Convert<System.Drawing.Color>(color);
            }
            catch(Exception) {
                return System.Drawing.Color.White;
            }
        }

        public static MessageBuilder User(this MessageBuilder builder, User user, Func<User, long> avatarid) {
            return User(builder, user, avatarid(user));
        }

        public static MessageBuilder User(this MessageBuilder builder, User user, long avatarid=-1) {
            if(avatarid > 0)
                builder.Image(avatarid);
            return builder.Bold().Color(ColorOrDefault(user.Color).FixColor()).Text(user.Name).Reset();
        }

        /// <summary>
        /// fixes color to represent a minimum brightness
        /// </summary>
        /// <param name="color">color to fix</param>
        /// <param name="minbrightness">minimum brightness for a color to have</param>
        /// <returns>color value</returns>
        public static Color FixColor(this Color color, float minbrightness = 0.5f) {
            float value = color.R + color.G + color.B;
            if(value >= 768 * minbrightness)
                return color;

            int basevalue = (int)(256 * minbrightness);
            return Color.FromRgb((byte)(basevalue + color.R * (1.0f - minbrightness)), (byte)(basevalue + color.G * (1.0f - minbrightness)), (byte)(basevalue + color.B * (1.0f - minbrightness)));
        }

        /// <summary>
        /// fixes color to represent a minimum brightness
        /// </summary>
        /// <param name="color">color to fix</param>
        /// <param name="minbrightness">minimum brightness for a color to have</param>
        /// <returns>color value</returns>
        public static System.Drawing.Color FixColor(this System.Drawing.Color color, float minbrightness = 0.5f)
        {
            float value = color.R + color.G + color.B;
            if (value >= 768 * minbrightness)
                return color;

            int basevalue = (int)(256 * minbrightness);
            return System.Drawing.Color.FromArgb((byte)(basevalue + color.R * (1.0f - minbrightness)), (byte)(basevalue + color.G * (1.0f - minbrightness)), (byte)(basevalue + color.B * (1.0f - minbrightness)));
        }

        public static string CreateEnumeration(this IEnumerable<string> objects) {
            if(!objects.Any())
                return "";

            StringBuilder sb=new StringBuilder(objects.First());
            if(objects.Count() > 1) {
                foreach(string o in objects.Skip(1).Take(objects.Count()-2))
                    sb.Append(", ").Append(o);
                sb.Append(" and ").Append(objects.Last());
            }
            return sb.ToString();
        }

        static int LastIndexOf(string message, char character, int startindex, int length) {
            if(startindex + length > message.Length)
                length = message.Length - startindex;

            for(int i=startindex+length-1; i>=startindex;--i)
                if(message[i] == character)
                    return i;
            return -1;
        }

        /// <summary>
        /// splits a message preferably at spaces
        /// </summary>
        /// <param name="message">message to split</param>
        /// <param name="maxlength">maximum length of a single message</param>
        /// <returns>enumeration of messages split from the original message</returns>
        public static IEnumerable<string> SplitMessage(this string message, int maxlength) {
            if (message.Length <= maxlength)
            {
                yield return message;
                yield break;
            }

            int startoffset = 0;
            do
            {
                if(message.Length - startoffset <= maxlength) {
                    yield return message.Substring(startoffset);
                    break;
                }

                int index = LastIndexOf(message, ' ', startoffset, maxlength);
                if (index == -1) {
                    yield return message.Substring(startoffset, Math.Min(maxlength, message.Length - startoffset));
                    startoffset += maxlength;
                }
                else
                {
                    yield return message.Substring(startoffset, index - startoffset);
                    startoffset = index + 1;
                }
            }
            while (startoffset < message.Length);
        }
    }
}