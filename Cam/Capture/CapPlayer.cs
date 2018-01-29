using System;
using System.Windows.Controls;

namespace StreamRC.Cam.Capture
{
    public class CapPlayer : Image,IDisposable {
        CapDevice _device;
        string devicename;
        int frameDelay;

        /// <summary>
        /// device to display
        /// </summary>
        public string Device
        {
            get { return devicename; }
            set
            {
                if(devicename == value)
                    return;

                devicename = value;
                if(devicename != null)
                    OpenDevice(devicename, frameDelay);
            }
        }

        /// <summary>
        /// number of frames to buffer before displaying frame
        /// </summary>
        public int FrameDelay
        {
            get { return frameDelay; }
            set
            {
                int nframes = Math.Max(0, value);
                if(nframes == frameDelay)
                    return;

                frameDelay = nframes;
                if(devicename != null)
                    OpenDevice(devicename, frameDelay);
            }
        }

        void OpenDevice(string device, int frames) {
            _device?.Dispose();

            _device = new CapDevice(device, frames);
            _device.OnNewBitmapReady += _device_OnNewBitmapReady;
        }

        void _device_OnNewBitmapReady(object sender, EventArgs e)
        {
            Source = _device.BitmapSource;
        }

        public void Dispose() {
            _device?.Dispose();
        }
    }
}
