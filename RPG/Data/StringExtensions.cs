using System.Linq;

namespace StreamRC.RPG.Data {

    /// <summary>
    /// extension methods for plain strings
    /// </summary>
    public static class StringExtensions {

        public static bool IsNumber(this string data) {
            return data.All(c => char.IsDigit(c) || c == '-');
        }
    }
}