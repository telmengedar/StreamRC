using System;
using System.ComponentModel;
using System.Diagnostics;
using NightlyCode.Core.Logs;
using NightlyCode.Japi.Json;
using StreamRC.Core.Settings;
using StreamRC.Core.UI;

namespace StreamRC.Cam {

    public class CameraModule : IDisposable {
        readonly ISettings settings;

        Process currentprocess;

        public CameraModule(ISettings settings, IMainWindow mainwindow) {
            this.settings = settings;
            Start(mainwindow);
        }

        void Start(IMainWindow mainwindow) {
            try {
                CameraDevice device = JSON.Read<CameraDevice>(settings.Get<string>(this, "lastdevice"));
                if (device != null)
                    ShowDevice(device);
            }
            catch (Exception e) {
                
            }

            mainwindow.AddMenuItem("Display.Camera", (sender, args) => {
                CameraPortal portal = new CameraPortal();
                portal.Closing += OnPortalClosing;
                portal.ShowDialog();
            });
        }

        void OnPortalClosing(object sender, CancelEventArgs e) {
            CameraDevice device = ((CameraPortal)sender).SelectedDevice;
            if(device != null) {
                try {
                    ShowDevice(device);
                }
                catch(Exception ex) {
                    Logger.Error(this, "Unable to open camera window", ex);
                }
            }
        }

        void ShowDevice(CameraDevice device) {
            if(currentprocess != null && !currentprocess.HasExited) {
                try {
                    currentprocess.Kill();
                }
                catch(Exception ex) {
                    Logger.Error(this, "Unable to close existing camera process", ex);
                }
            }

            settings.Set(this, "lastdevice", JSON.WriteString(device));

            ProcessStartInfo startinfo = new ProcessStartInfo($".\\modules\\cam\\CameraDisplay_{device.Platform.ToString().ToLower()}.exe", $"\"{device.Device}\"") {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            currentprocess = Process.Start(startinfo);
            if(!(currentprocess?.HasExited??false))
                currentprocess.Exited += OnCameraClosed;
            else {
                currentprocess = null;
            }
        }

        void OnCameraClosed(object sender, EventArgs e) {
            settings.Set(this, "lastdevice", null);
            currentprocess = null;
        }

        void IDisposable.Dispose() {
            currentprocess?.Kill();
            currentprocess?.Dispose();
        }
    }
}