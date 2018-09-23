using System;
using System.ComponentModel;
using System.Diagnostics;
using NightlyCode.Core.Logs;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Cam {

    public class CameraModule : IRunnableModule {
        Context context;

        Process currentprocess;
        CameraDevice current;

        public CameraModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            try {
                CameraDevice device = JSON.Read<CameraDevice>(context.Settings.Get<string>(this, "lastdevice"));
                if (device != null)
                    ShowDevice(device);
            }
            catch (Exception e) {
                
            }

            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Display.Camera", (sender, args) => {
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

        void IRunnableModule.Stop() {
            if(currentprocess != null)
                currentprocess.Kill();
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

            current = device;
            context.Settings.Set(this, "lastdevice", JSON.WriteString(device));

            ProcessStartInfo startinfo = new ProcessStartInfo($".\\modules\\cam\\CameraDisplay_{device.Platform.ToString().ToLower()}.exe", $"\"{device.Device}\"") {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            currentprocess = Process.Start(startinfo);
            if(!currentprocess.HasExited)
                currentprocess.Exited += OnCameraClosed;
            else {
                current = null;
                currentprocess = null;
            }
        }

        private void OnCameraClosed(object sender, EventArgs e) {
            context.Settings.Set(this, "lastdevice", null);
            current = null;
            currentprocess = null;
        }
    }
}