using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StreamRC.Streaming.Stream {
    public static class MessageExtensions {

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