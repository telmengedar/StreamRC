using System.Text;

namespace StreamRC.Core.Http {

    /// <summary>
    /// extensions for http data
    /// </summary>
    public static class HttpExtensions {

        /// <summary>
        /// encodes data for usage in url string
        /// </summary>
        /// <param name="data">data to encode</param>
        /// <returns>string usable in query strings</returns>
        public static string URLEncode(this string data)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char character in data)
            {
                if (char.IsLetterOrDigit(character))
                    builder.Append(character);
                else if (character == ' ')
                    builder.Append('+');
                else
                    builder.Append($"%{(int)character:D2}");
            }
            return builder.ToString();
        }
    }
}