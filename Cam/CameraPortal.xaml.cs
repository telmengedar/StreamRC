using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using NightlyCode.Japi.Json;

namespace StreamRC.Cam
{
    /// <summary>
    /// Interaction logic for CameraPortal.xaml
    /// </summary>
    public partial class CameraPortal : Window
    {
        public CameraPortal()
        {
            InitializeComponent();
            PopulateCameras();
        }

        public CameraDevice SelectedDevice
        {
            get
            {
                return cmbCameras.SelectedItem as CameraDevice;
            }
        }

        IEnumerable<DeviceInfo> GetCameraList(Platform platform) {
            string process = $".\\modules\\cam\\CameraDisplay_{platform.ToString().ToLower()}.exe";

            ProcessStartInfo startinfo = new ProcessStartInfo() {
                FileName = process,
                Arguments = "--list",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            Process currentprocess = Process.Start(startinfo);
            string result = currentprocess.StandardOutput.ReadToEnd();
            return JSON.Read<DeviceInfo[]>(result);
        }

        void PopulateCameras() {
            try { 
                foreach(DeviceInfo device in GetCameraList(Platform.X86))
                    cmbCameras.Items.Add(new CameraDevice() {
                        Device = device.ID,
                        Display = device.Name,
                        Platform = Platform.X86
                    });
            }
            catch (Exception e)
            {
            }

            try { 
                foreach (DeviceInfo device in GetCameraList(Platform.X64))
                    cmbCameras.Items.Add(new CameraDevice()
                    {
                        Device = device.ID,
                        Display = device.Name,
                        Platform = Platform.X64
                    });
            }
            catch (Exception e)
            {
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
