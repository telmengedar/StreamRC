using System.Text;

namespace NightlyCode.StreamRC.Gangolf {
    public static class StringExtensions {

        /// <summary>
        /// converts an input to a string with all name strings beginning in upper casing
        /// </summary>
        /// <param name="input">input to convert</param>
        /// <returns>string with all name strings beginning in upper casing</returns>
        public static string Namify(this string input) {
            StringBuilder sb=new StringBuilder();
            bool first = true;
            foreach(char character in input) {
                if(!char.IsLetter(character)) {
                    first = true;
                    sb.Append(character);
                }
                else {
                    sb.Append((char)(character & (first ? 0xDF : 0xFF)));
                    first = false;
                }
            }

            return sb.ToString();
        }
    }
}