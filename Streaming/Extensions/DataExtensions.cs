using System;
using System.Windows.Media;

namespace StreamRC.Streaming.Extensions {

    /// <summary>
    /// extensions for data
    /// </summary>
    public static class DataExtensions {

        /// <summary>
        /// parses a color from string
        /// </summary>
        /// <param name="color">string containing color data</param>
        /// <returns>color parsed from string or <see cref="Colors.White"/> if data can't be parsed</returns>
        public static Color ParseColor(this string color) {
            if(string.IsNullOrEmpty(color))
                return Colors.White;

            try {
                return (Color)ColorConverter.ConvertFromString(color);
            }
            catch(Exception) {
                return Colors.White;
            }
        }
    }
}