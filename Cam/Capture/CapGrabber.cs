using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace StreamRC.Cam.Capture
{
    internal class CapGrabber : ISampleGrabberCB {
        readonly int bufferedpictures;
        int currentpicture;

        readonly IntPtr section;
        readonly IntPtr picturebuffer;
        readonly int picturelength;

        public CapGrabber(int width, int height, int bufferedpictures) {
            picturelength=width * height * PixelFormats.Bgr32.BitsPerPixel / 8;

            section = Native.CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, (uint)(picturelength*(bufferedpictures+1)), null);
            picturebuffer = Native.MapViewOfFile(section, 0xF001F, 0, 0, (uint)(picturelength * (bufferedpictures + 1)));

            this.bufferedpictures = bufferedpictures;
        }

        public IntPtr Memory => section;

        public IntPtr Picture => picturebuffer;

        public int SampleCB(double sampleTime, IntPtr sample)
        {
            return 0;
        }

        public int BufferCB(double sampleTime, IntPtr buffer, int bufferLen) {
            if(bufferedpictures == 0) {
                CopyMemory(picturebuffer, buffer, bufferLen);
                return 0;
            }

            IntPtr targetptr = new IntPtr(picturebuffer.ToInt64() + picturelength * (currentpicture + 1));
            CopyMemory(targetptr, buffer, bufferLen);
            CopyMemory(picturebuffer, new IntPtr(picturebuffer.ToInt64() + picturelength * (1 + (currentpicture + 1) % bufferedpictures)), picturelength);
            OnNewFrameArrived();

            currentpicture = (currentpicture + 1) % bufferedpictures;
            return 0;
        }

        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        public event EventHandler NewFrameArrived;
        void OnNewFrameArrived()
        {
            NewFrameArrived?.Invoke(this, null);
        }
    }
}
