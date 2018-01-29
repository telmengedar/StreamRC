using System.Runtime.InteropServices;

namespace StreamRC.Cam.Capture {
    [ComVisible(false), StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;

        public int Top;

        public int Right;

        public int Bottom;
    }
}