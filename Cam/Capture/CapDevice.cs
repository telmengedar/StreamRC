using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace StreamRC.Cam.Capture
{
    internal class CapDevice:DependencyObject,IDisposable
    {

        ManualResetEvent stopSignal;
        Thread worker;
        IGraphBuilder graph;
        ISampleGrabber grabber;
        IBaseFilter sourceObject, grabberObject;
        IMediaControl control;
        CapGrabber capGrabber;
        readonly int delay;
        readonly string deviceMoniker;

        public InteropBitmap BitmapSource
        {
            get { return (InteropBitmap)GetValue(BitmapSourceProperty); }
            private set { SetValue(BitmapSourcePropertyKey, value); }
        }

        static readonly DependencyPropertyKey BitmapSourcePropertyKey = DependencyProperty.RegisterReadOnly("BitmapSource", typeof(InteropBitmap), typeof(CapDevice), new UIPropertyMetadata(default(InteropBitmap)));
        public static readonly DependencyProperty BitmapSourceProperty = BitmapSourcePropertyKey.DependencyProperty;

        public CapDevice(string device, int delay)
        {
            this.delay = delay;
            deviceMoniker = device;
            Start();
        }

        public void Start()
        {
            if (worker == null)
            {
                stopSignal = new ManualResetEvent(false);
                worker = new Thread(RunWorker);
                worker.Start();
            }
            else
            {
                Stop();
                Start();
            }
        }

        void capGrabber_NewFrameArrived(object sender, EventArgs e) {
            Dispatcher?.Invoke(System.Windows.Threading.DispatcherPriority.Render, (SendOrPostCallback)delegate {
                BitmapSource?.Invalidate();
            }, null);
        }

        public void Stop()
        {
            if (IsRunning)
            {
                stopSignal.Set();
                worker.Abort();
                if (worker != null)
                {
                    worker.Join();
                    Release();
                }
            }
        }

        public bool IsRunning
        {
            get
            {
                if (worker != null)
                {
                    if (worker.Join(0) == false)
                        return true;

                    Release();
                }
                return false;
            }
        }

        void Release()
        {
            worker = null;

            stopSignal.Close();
            stopSignal = null;
        }

        void RunWorker()
        {
            try
            {
                
                graph = (IGraphBuilder)Activator.CreateInstance(Type.GetTypeFromCLSID(FilterGraph));
                
                sourceObject = FilterInfo.CreateFilter(deviceMoniker);

                grabber = (ISampleGrabber)Activator.CreateInstance(Type.GetTypeFromCLSID(SampleGrabber));
                grabberObject = grabber as IBaseFilter;

                graph.AddFilter(sourceObject, "source");
                graph.AddFilter(grabberObject, "grabber");

                using (AMMediaType mediaType = new AMMediaType())
                {
                    mediaType.MajorType = MediaTypes.Video;
                    mediaType.SubType = MediaSubTypes.RGB32;
                    grabber.SetMediaType(mediaType);

                    if (graph.Connect(sourceObject.GetPin(PinDirection.Output, 0), grabberObject.GetPin(PinDirection.Input, 0)) >= 0)
                    {
                        if (grabber.GetConnectedMediaType(mediaType) == 0)
                        {
                            VideoInfoHeader header = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.FormatPtr, typeof(VideoInfoHeader));
                            capGrabber = new CapGrabber(header.BmiHeader.Width, header.BmiHeader.Height, delay);
                            capGrabber.NewFrameArrived += capGrabber_NewFrameArrived;
                            Dispatcher.Invoke(() => {
                                BitmapSource = Imaging.CreateBitmapSourceFromMemorySection(capGrabber.Memory, header.BmiHeader.Width, header.BmiHeader.Height, PixelFormats.Bgr32, header.BmiHeader.Width * PixelFormats.Bgr32.BitsPerPixel / 8, 0) as InteropBitmap;
                                OnNewBitmapReady?.Invoke(this, null);
                            });
                        }
                    }
                    graph.Render(grabberObject.GetPin(PinDirection.Output, 0));
                    grabber.SetBufferSamples(false);
                    grabber.SetOneShot(false);
                    grabber.SetCallback(capGrabber, 1);

                    IVideoWindow wnd = (IVideoWindow)graph;
                    wnd.put_AutoShow(false);

                    control = (IMediaControl)graph;
                    control.Run();

                    while (!stopSignal.WaitOne(0, true))
                    {
                        Thread.Sleep(10);
                    }

                    control.StopWhenReady();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                graph = null;
                sourceObject = null;
                grabberObject = null;
                grabber = null;
                capGrabber = null;
                control = null;
                
            }
            
        }

        static readonly Guid FilterGraph = new Guid(0xE436EBB3, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

        static readonly Guid SampleGrabber = new Guid(0xC1F400A0, 0x3F08, 0x11D3, 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37);

        public event EventHandler OnNewBitmapReady;


        public void Dispose()
        {
            Stop();
        }
    }
}
