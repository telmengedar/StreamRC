using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Japi.Json;

namespace StreamRC.Streaming.Text.Font {

    /// <summary>
    /// fontset used to draw characters
    /// </summary>
    public class FontSet {
        readonly Image atlas;
        readonly Dictionary<char, Glyph> glyphs=new Dictionary<char, Glyph>();
        readonly float fontsize;
        readonly float fontspacing;
        readonly bool uppercase;

        /// <summary>
        /// creates a new <see cref="FontSet"/>
        /// </summary>
        /// <param name="stream">stream containing fontset description</param>
        public FontSet(System.IO.Stream stream) {
            FontDescription description = JSON.Read<FontDescription>(stream);

            
            fontsize = description.Size;
            fontspacing = description.Spacing;
            uppercase = description.Uppercase;
            
            switch(description.Type) {
                case FontSourceType.Resource:
                    atlas = Image.FromStream(ResourceAccessor.GetResource<System.IO.Stream>(description.Path));
                    break;
                default:
                    throw new Exception("Unsupported sourcetype");
            }

            foreach(Glyph glyph in description.Characters)
                glyphs[glyph.Character[0]] = glyph;
        }

        public float FontSize => fontsize;

        int ComputeSpacing(FontSpacing lhs, FontSpacing rhs) {
            if(lhs == null)
                return 0;
            return Math.Min(lhs.BottomRight + rhs.BottomLeft, Math.Min(lhs.CenterRight + rhs.CenterLeft, lhs.TopRight + rhs.TopLeft));
        }

        int MeasureWidth(string text, double size) {
            double factor = size / fontsize;
            double width = -fontspacing * factor;
            FontSpacing lastspacing = null;
            foreach(char character in text) {
                if(glyphs.ContainsKey(character)) {
                    Glyph glyph = glyphs[character];
                    width -= ComputeSpacing(lastspacing, glyph.Spacing);
                    width += fontspacing * factor;
                    width += glyph.Width * factor;
                    lastspacing = glyph.Spacing;
                }
            }

            return (int)Math.Max(0, width);
        }

        /// <summary>
        /// draws a text to an image
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="size">font size</param>
        /// <param name="target">image to draw to</param>
        /// <param name="x">target x</param>
        /// <param name="y">target y</param>
        public void DrawText(string text, float size, Image target, float x = 0.0f, float y = 0.0f) {
            DrawText(text, size, target, Color.White, x, y);
        }

        /// <summary>
        /// draws a text to an image
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="size">font size</param>
        /// <param name="target">image to draw to</param>
        /// <param name="blend">blend color</param>
        /// <param name="x">target x</param>
        /// <param name="y">target y</param>
        public void DrawText(string text, float size, Image target, Color blend, float x = 0.0f, float y = 0.0f) {
            if(uppercase)
                text = text.ToUpper();

            ColorMatrix colormatrix = new ColorMatrix(new[] {
                new[] {blend.R / 255.0f, 0.0f, 0.0f, 0.0f, 0.0f},
                new[] {0.0f, blend.G / 255.0f, 0.0f, 0.0f, 0.0f},
                new[] {0.0f, 0.0f, blend.B / 255.0f, 0.0f, 0.0f},
                new[] {0.0f, 0.0f, 0.0f, blend.A / 255.0f, 0.0f},
                new[] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f}
            });

            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colormatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            float factor = size / fontsize;
            FontSpacing lastspacing = null;

            using (Graphics g = Graphics.FromImage(target)) {
                foreach(char character in text) {
                    if(glyphs.ContainsKey(character)) {
                        Glyph glyph = glyphs[character];
                        x -= ComputeSpacing(lastspacing, glyph.Spacing);
                        g.DrawImage(atlas,
                            new Rectangle((int)x, (int)(y + size - glyph.Height * factor), (int)(glyph.Width * factor), (int)(glyph.Height * factor)),
                            glyph.X, glyph.Y, glyph.Width, (float)glyph.Height,
                            GraphicsUnit.Pixel, attributes);
                        x += (glyph.Width + fontspacing) * factor;
                        lastspacing = glyph.Spacing;
                    }
                }
            }
        }

        /// <summary>
        /// generates an image from a text
        /// </summary>
        /// <param name="text">text to generate</param>
        /// <param name="size">size of font</param>
        /// <returns>image with generated text</returns>
        public Image DrawText(string text, float size) {
            return DrawText(text, size, Color.White);
        }

        /// <summary>
        /// generates an image from a text
        /// </summary>
        /// <param name="text">text to generate</param>
        /// <param name="size">size of font</param>
        /// <param name="color">text color</param>
        /// <param name="padx">number of pixels to pad horizontally</param>
        /// <param name="pady">number of pixels to pad vertically</param>
        /// <returns>image with generated text</returns>
        public Image DrawText(string text, float size, Color color, int padx=0, int pady=0) {
            if (uppercase)
                text = text.ToUpper();

            Bitmap bitmap = new Bitmap(MeasureWidth(text, size) + padx * 2, (int)size + pady * 2);
            DrawText(text, size, bitmap, color, padx, pady);
            return bitmap;
        }
    }
}