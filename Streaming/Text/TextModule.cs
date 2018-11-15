using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using NightlyCode.Core.Collections.Cache;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using StreamRC.Streaming.Text.Font;

namespace StreamRC.Streaming.Text {

    /// <summary>
    /// module used to generate text
    /// </summary>
    [Module]
    public class TextModule {
        readonly FontSet fontset;
        readonly ImageToolset toolset=new ImageToolset();
        readonly TimedCache<TextSpecs, byte[]> imagecache;
         
        /// <summary>
        /// creates a new <see cref="TextModule"/>
        /// </summary>
        public TextModule() {
            imagecache = new TimedCache<TextSpecs, byte[]>(CreateText);
            fontset = new FontSet(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Text.Font.CyFont.json"));
        }

        byte[] CreateText(TextSpecs text) {
            Image image = fontset.DrawText(text.Text, text.Size, text.Color, text.OutlineThickness, text.OutlineThickness);

            if(text.OutlineThickness > 0) {
                Image outline = toolset.CreateOutline(image, text.OutlineThickness, text.OutlineColor);
                using(Graphics g = Graphics.FromImage(outline)) {
                    g.DrawImage(image, 0, 0);
                }
                image = outline;
            }

            using(MemoryStream ms = new MemoryStream()) {
                image.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// creates an image out of a text
        /// </summary>
        /// <param name="text">text to create</param>
        /// <param name="fontsize">size to use for font</param>
        /// <returns></returns>
        public byte[] CreateTextImage(string text, float fontsize) {
            return imagecache[new TextSpecs(text, fontsize)];
        }

        /// <summary>
        /// creates an image out of a text
        /// </summary>
        /// <param name="text">text to create</param>
        /// <param name="fontsize">size to use for font</param>
        /// <param name="color">blend color to use</param>
        /// <returns></returns>
        public byte[] CreateTextImage(string text, float fontsize, Color color, Color outlinecolor, int thickness=1)
        {
            return imagecache[new TextSpecs(text, fontsize, color) {
                OutlineColor = outlinecolor,
                OutlineThickness = thickness
            }];
        }
    }
}