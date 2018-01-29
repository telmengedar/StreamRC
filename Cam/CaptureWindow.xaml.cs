using System;
using System.Linq;
using System.Windows;
using StreamRC.Cam.Capture;

namespace StreamRC.Cam
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class CaptureWindow : Window
    {
        public CaptureWindow()
        {
            InitializeComponent();
            cmbDevices.ItemsSource = CaptureDevice.Devices.Select(d=>d.MonikerString).ToArray();
        }

        void txtFrames_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try {
                if(txtFrames.Text.All(char.IsDigit))
                    player.FrameDelay = int.Parse(txtFrames.Text);
                else {
                    TimeSpan time = TimeSpan.Parse(txtFrames.Text);
                    player.FrameDelay = (int)(time.TotalSeconds * 30);
                    txtFrames.Text = player.FrameDelay.ToString();
                }
            }
            catch(Exception) {
                
                throw;
            }
            
        }

        void cmbDevices_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            player.Device = cmbDevices.SelectedItem.ToString();
        }
    }
}
