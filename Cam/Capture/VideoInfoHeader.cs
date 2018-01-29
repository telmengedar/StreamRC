using System.Runtime.InteropServices;

namespace StreamRC.Cam.Capture {
    [ComVisible(false), StructLayout(LayoutKind.Sequential)]
    internal struct VideoInfoHeader
    {
        public RECT SrcRect;

        public RECT TargetRect;

        public int BitRate;

        public int BitErrorRate;

        public long AverageTimePerFrame;

        public BitmapInfoHeader BmiHeader;
    }
}