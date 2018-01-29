using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace StreamRC.Cam.Capture {
    public class CaptureDevice {
        public static readonly Guid SystemDeviceEnum = new Guid(0x62BE5D10, 0x60EB, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

        public static readonly Guid VideoInputDevice = new Guid(0x860BB310, 0x5D01, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

        /// <summary>
        /// list of device infos
        /// </summary>
        public static IEnumerable<FilterInfo> Devices
        {
            get
            {
                IMoniker[] ms = new IMoniker[1];
                ICreateDevEnum enumD = (ICreateDevEnum)Activator.CreateInstance(Type.GetTypeFromCLSID(SystemDeviceEnum));
                IEnumMoniker moniker;
                Guid g = VideoInputDevice;
                if (enumD.CreateClassEnumerator(ref g, out moniker, 0) == 0)
                {

                    while (true)
                    {
                        int r = moniker.Next(1, ms, IntPtr.Zero);
                        if (r != 0 || ms[0] == null)
                            break;
                        yield return new FilterInfo(ms[0]);
                        Marshal.ReleaseComObject(ms[0]);
                        ms[0] = null;

                    }
                }
            }
        }

    }
}