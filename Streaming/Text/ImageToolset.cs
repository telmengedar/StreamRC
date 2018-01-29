using System;
using System.Drawing;
using StreamRC.Streaming.Text.Images;

namespace StreamRC.Streaming.Text {

    /// <summary>
    /// toolset to process images
    /// </summary>
    public class ImageToolset {

        /// <summary>
        /// creates an outline for the specified image
        /// </summary>
        /// <param name="image">image for which to create outline</param>
        /// <param name="thickness">outline thickness</param>
        /// <returns>image containing outline</returns>
        public Image CreateOutline(Image image, int thickness = 1) {
            return CreateOutline(image, thickness, Color.Black);
        }

        /// <summary>
        /// creates an outline for the specified image
        /// </summary>
        /// <param name="image">image for which to create outline</param>
        /// <param name="thickness">outline thickness</param>
        /// <param name="color">color of outline</param>
        /// <returns>image containing outline</returns>
        public Image CreateOutline(Image image, int thickness, Color color) {
            Bitmap outline = new Bitmap(image.Width, image.Height);

            float threshold = 3.0f;

            using(FastBitmap outimage = new FastBitmap(outline)) {
                using(FastBitmap inimage = new FastBitmap(image as Bitmap)) {
                    for(int y = 0; y < image.Height; ++y)
                        for(int x = 0; x < image.Width; ++x) {
                            if(inimage.GetPixel(x, y).Alpha == 255)
                                continue;

                            float neighbours = 0.0f;

                            for(int dy=-thickness;dy<=thickness;++dy)
                                for(int dx = -thickness; dx <= thickness; ++dx) {
                                    int tx = x + dx;
                                    int ty = y + dy;
                                    if(tx < 0 || tx >= image.Width || ty < 0 || ty >= image.Height)
                                        continue;

                                    if(inimage.GetPixel(tx, ty).Alpha == 255)
                                        neighbours += 1.0f;
                                }

                            if(neighbours>0.0f)
                                outimage.SetPixel(x, y, (byte)(Math.Min(1.0f, neighbours/threshold)*color.A), color.R, color.G, color.B);
                        }
                }
            }

            return outline;
        }


    }
}