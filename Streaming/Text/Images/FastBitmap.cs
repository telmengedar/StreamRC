using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace StreamRC.Streaming.Text.Images
{

    /// <summary>
    /// bitmap allowing fast pixel operations
    /// </summary>
    public unsafe class FastBitmap : IDisposable
    {
        readonly System.Drawing.Bitmap bitmap;

        // three elements used for MakeGreyUnsafe
        int width;
        BitmapData bitmapData;
        byte* pBase = null;

        /// <summary>
        /// creates a new <see cref="FastBitmap"/>
        /// </summary>
        /// <param name="bitmap"></param>
        public FastBitmap(System.Drawing.Bitmap bitmap)
        {
            this.bitmap = bitmap;
            LockBitmap();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            UnlockBitmap();
        }

        void LockBitmap()
        {
            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF boundsF = bitmap.GetBounds(ref unit);
            Rectangle bounds = new Rectangle((int)boundsF.X,
                (int)boundsF.Y,
                (int)boundsF.Width,
                (int)boundsF.Height);

            // Figure out the number of bytes in a row
            // This is rounded up to be a multiple of 4
            // bytes, since a scan line in an image must always be a multiple of 4 bytes
            // in length. 
            int pixelsize;
            switch (bitmap.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    pixelsize = 3;
                    break;
                case PixelFormat.Format32bppArgb:
                    pixelsize = 4;
                    break;
                default:
                    throw new NotSupportedException();
            }

            width = (int)boundsF.Width * pixelsize;
            if (width % 4 != 0)
            {
                width = 4 * (width / 4 + 1);
            }
            bitmapData = bitmap.LockBits(bounds, ImageLockMode.ReadWrite, bitmap.PixelFormat);

            pBase = (byte*)bitmapData.Scan0.ToPointer();
        }

        public Pixel GetPixel(int x, int y)
        {
            switch (bitmap.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    PixelData24 pixel24 = *PixelAt24(x, y);
                    return new Pixel(255, pixel24.red, pixel24.green, pixel24.blue);
                case PixelFormat.Format32bppArgb:
                    PixelData32 pixel32 = *PixelAt32(x, y);
                    return new Pixel(pixel32.alpha, pixel32.red, pixel32.green, pixel32.blue);
                default:
                    throw new NotSupportedException();
            }
        }

        public void SetPixel(int x, int y, byte red, byte green, byte blue)
        {
            SetPixel(x, y, 255, red, green, blue);
        }

        public void SetPixel(int x, int y, byte alpha, byte red, byte green, byte blue)
        {
            switch (bitmap.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    PixelData24* pixel24 = PixelAt24(x, y);
                    *pixel24 = new PixelData24(red, green, blue);
                    break;
                case PixelFormat.Format32bppArgb:
                    PixelData32* pixel32 = PixelAt32(x, y);
                    *pixel32 = new PixelData32(alpha, red, green, blue);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        void UnlockBitmap()
        {
            bitmap.UnlockBits(bitmapData);
            bitmapData = null;
            pBase = null;
        }

        PixelData24* PixelAt24(int x, int y)
        {
            return (PixelData24*)(pBase + y * width + x * sizeof(PixelData24));
        }

        PixelData32* PixelAt32(int x, int y)
        {
            return (PixelData32*)(pBase + y * width + x * sizeof(PixelData32));
        }
    }
}

