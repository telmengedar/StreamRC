using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace StreamRC.Core.Messages {

    /// <summary>
    /// extensions for messages
    /// </summary>
    public static class MessageExtensions {

        static string ExtractField(string text, ref int index, char terminator)
        {
            int startindex = index;
            while (index < text.Length && text[index] != terminator)
                ++index;
            return text.Substring(startindex, index - startindex);
        }

        static Color TranslateColor(System.Drawing.Color color) {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        static void ProcessField(string field, ref FontWeight weight, ref Color color, ref string alternative, object[] arguments) {
            string[] split = field.Split('=');
            switch(split[0].ToLower()) {
                case "alt":
                    alternative = split[1].StartsWith("$") ? arguments[int.Parse(split[1].Substring(1))].ToString() : split[1];
                    break;
                case "c":
                case "color":
                    string colorvalue;
                    if(split[1].StartsWith("$")) {
                        object argumentcolor = arguments[int.Parse(split[1].Substring(1))];
                        if(argumentcolor is Color) {
                            color = (Color)argumentcolor;
                            return;
                        }

                        if(argumentcolor is System.Drawing.Color) {
                            color = TranslateColor((System.Drawing.Color)argumentcolor);
                            return;
                        }
                        colorvalue = argumentcolor.ToString();
                    }
                    else colorvalue = split[1];
                    if(colorvalue.StartsWith("#"))
                        colorvalue = colorvalue.Substring(1);
                    color = Color.FromRgb(byte.Parse(colorvalue.Substring(0, 2), NumberStyles.HexNumber), byte.Parse(colorvalue.Substring(2, 2), NumberStyles.HexNumber), byte.Parse(colorvalue.Substring(4, 2), NumberStyles.HexNumber));
                    break;
                case "b":
                case "bold":
                    weight=FontWeight.Bold;
                    break;
                case "d":
                case "default":
                    weight=FontWeight.Default;
                    break;
                case "t":
                case "thin":
                    weight=FontWeight.Thin;
                    break;
                case "n":
                case "normal":
                    weight=FontWeight.Normal;
                    break;
                case "/":
                    weight=FontWeight.Default;
                    color = Colors.White;
                    alternative = "";
                    break;
            }
        }

        public static string GetMultiple(this string name)
        {
            if (name.EndsWith("y"))
                return name.Substring(0, name.Length - 1) + "ies";

            if (name.EndsWith("ch"))
                return name + "es";

            return name + "s";
        }

        static string TranslateContent(string content, params object[] arguments) {
            if(string.IsNullOrEmpty(content))
                return "";

            if(!content.StartsWith("$"))
                return content;

            int index = int.Parse(content.Substring(1));
            return arguments[index]?.ToString() ?? "";
        }

        public static IEnumerable<MessageChunk> ParseChunks(string statement, params object[] arguments) {
            FontWeight weight = FontWeight.Default;
            Color color = Colors.White;
            string alternative="";

            int startindex = 0;
            for (int i = 0; i < statement.Length; ++i)
            {
                switch (statement[i])
                {
                    case '[':
                        if(i > startindex)
                            yield return new MessageChunk(statement.Substring(startindex, i - startindex), color, weight) {
                                Alternative = alternative
                            };

                        ++i;
                        ProcessField(ExtractField(statement, ref i, ']'), ref weight, ref color, ref alternative, arguments);
                        startindex = i + 1;
                        break;
                    case '$':
                        if(i > startindex)
                            yield return new MessageChunk(statement.Substring(startindex, i - startindex), color, weight) {
                                Alternative = alternative
                            };

                        ++i;
                        StringBuilder numberbuilder=new StringBuilder();
                        while(char.IsDigit(statement[i]))
                            numberbuilder.Append(statement[i++]);
                        --i;

                        if(numberbuilder.Length > 0)
                            yield return new MessageChunk(arguments[int.Parse(numberbuilder.ToString())].ToString(), color, weight) {
                                Alternative = alternative
                            };

                        startindex = i + 1;
                        break;
                    case '<':
                        if(i > startindex)
                            yield return new MessageChunk(statement.Substring(startindex, i - startindex), color, weight) {
                                Alternative = alternative
                            };

                        ++i;
                        string content = TranslateContent(ExtractField(statement, ref i, '>'), arguments);
                        if(!string.IsNullOrEmpty(content) && !content.StartsWith("-"))
                            yield return new MessageChunk(MessageChunkType.Emoticon, content) {
                                Alternative = alternative
                            };
                        startindex = i + 1;
                        break;
                }
            }

            if(startindex < statement.Length)
                yield return new MessageChunk(statement.Substring(startindex, statement.Length - startindex), color, weight) {
                    Alternative = alternative
                };
        }

        /// <summary>
        /// parses messagechunks from 
        /// </summary>
        /// <param name="statement"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static Message Parse(string statement, params object[] arguments) {
            return new Message(ParseChunks(statement, arguments).ToArray());
        }

        public static string GetPreposition(this string noun, bool countable=true, bool first=false) {
            if (!countable)
                return "";

            switch (noun[0])
            {
                case 'a':
                case 'A':
                case 'e':
                case 'E':
                case 'i':
                case 'I':
                case 'o':
                case 'O':
                case 'u':
                case 'U':
                case 'y':
                case 'Y':
                    return first ? "An " : "an ";
                default:
                    return first ? "A " : "a ";
            }
        }
    }
}