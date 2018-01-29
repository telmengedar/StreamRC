using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NightlyCode.Core.Collections.Cache;
using NightlyCode.Core.Conversion;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Status
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    [Dependency(nameof(MessageModule), DependencyType.Type)]
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(UserModule), DependencyType.Type)]
    [Dependency(ModuleKeys.MainWindow, DependencyType.Key)]
    [Dependency(nameof(ImageCacheModule), DependencyType.Type)]
    [ModuleKey("status")]
    public partial class StatusWindow : ModuleWindow {
        readonly Context context;

        readonly object chatlock = new object();

        readonly TimedCache<long, BitmapImage> emotecache;

        bool alternate;

        /// <summary>
        /// creates a new <see cref="StatusWindow"/>
        /// </summary>
        /// <param name="context">module context</param>
        public StatusWindow(Context context)
            : base(context) {
            InitializeComponent();
            this.context = context;
            emotecache = new TimedCache<long, BitmapImage>(CreateImage);
            Closing += (sender, args) => {
                Visibility = Visibility.Hidden;
                args.Cancel = true;
            };

            IsVisibleChanged += OnVisibilityChanged;
        }

        void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e) {
            StreamModule streammodule = context.GetModule<StreamModule>();
            if (IsVisible) {
                streammodule.ChatMessage += OnMessageReceived;
                streammodule.Hosted += OnHost;
                streammodule.NewFollower += OnFollower;
                streammodule.NewSubscriber += OnSubscriber;
                streammodule.MicroPresent += OnMicroPresent;
                context.GetModule<MessageModule>().Message += OnStatusMessage;
            }
            else {
                streammodule.ChatMessage -= OnMessageReceived;
                streammodule.Hosted -= OnHost;
                streammodule.NewFollower -= OnFollower;
                streammodule.NewSubscriber -= OnSubscriber;
                streammodule.MicroPresent -= OnMicroPresent;
                context.GetModule<MessageModule>().Message -= OnStatusMessage;
            }
        }

        void OnMicroPresent(MicroPresent present) {
            if(!string.IsNullOrEmpty(present.Message))
                Dispatcher.Invoke(() => AddStatus($"{present.Username} has donated {present.Amount} {present.Currency} -> \"{present.Message}\""));
            else Dispatcher.Invoke(() => AddStatus($"{present.Username} has donated {present.Amount} {present.Currency}."));
        }

        void OnStatusMessage(Message message) {
            Dispatcher.Invoke(() => AddMessage(message));
        }

        void OnSubscriber(SubscriberInformation subscriber) {
            Dispatcher.Invoke(() => AddStatus($"New Subscriber {subscriber.Username} ({subscriber.PlanName})."));
        }

        void OnFollower(UserInformation follower) {
            Dispatcher.Invoke(() => AddStatus($"New Follower {follower.Username}."));
        }

        void OnHost(HostInformation host) {
            Dispatcher.Invoke(() => AddStatus($"New Host from {host.Channel} for {host.Viewers} viewers."));
        }

        void AddStatus(string message) {
            lock(chatlock) {
                RemoveOldEntries();

                Paragraph paragraph = new Paragraph {
                    Background = new SolidColorBrush(Colors.DarkBlue),
                    Margin = new Thickness(0.0)
                };

                paragraph.Inlines.Add(new Run(message) {
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.Khaki)
                });
                txtChat.Document.Blocks.Add(paragraph);
                paragraph.Loaded += paragraph_Loaded;
            }
        }

        void paragraph_Loaded(object sender, RoutedEventArgs e)
        {
            Paragraph paragraph = (Paragraph)sender;
            paragraph.Loaded -= paragraph_Loaded;
            paragraph.BringIntoView();
        }

        void OnMessageReceived(ChatMessage message) {
            if(message.IsWhisper || string.IsNullOrEmpty(message.Message) || message.Message.StartsWith("!"))
                return;

            Dispatcher.Invoke(() => AddMessage(message));
        }

        void RemoveOldEntries() {
            while(txtChat.Document.Blocks.Count > 64)
                txtChat.Document.Blocks.Remove(txtChat.Document.Blocks.FirstBlock);
        }

        Color FixColor(Color color) {
            float value = color.R + color.G + color.B;
            if(value >= 386)
                return color;

            return Color.FromRgb((byte)(128 + color.R / 2), (byte)(128 + color.G / 2), (byte)(128 + color.B / 2));
        }

        System.Windows.FontWeight TranslateWeight(Core.Messages.FontWeight weight) {
            switch(weight) {
                default:
                case Core.Messages.FontWeight.Normal:
                case Core.Messages.FontWeight.Default:
                    return FontWeights.Normal;
                case Core.Messages.FontWeight.Bold:
                    return FontWeights.Bold;
                case Core.Messages.FontWeight.Thin:
                    return FontWeights.Thin;
            }
        }

        Color TranslateColor(string color) {
            if(color == null)
                return Colors.White;

            System.Drawing.Color data = Converter.Convert<System.Drawing.Color>(color);
            return Color.FromArgb(data.A, data.R, data.G, data.B);
        }

        void AddMessage(Message message) {
            lock (chatlock)
            {
                RemoveOldEntries();

                Paragraph paragraph = new Paragraph
                {
                    Margin = new Thickness(0.0),
                    Background = new SolidColorBrush(alternate ? Color.FromRgb(64, 64, 64) : Colors.Black)
                };
                alternate = !alternate;

                foreach(MessageChunk chunk in message.Chunks) {
                    if(chunk.Type == MessageChunkType.Emoticon) {
                        long imageid = context.GetModule<ImageCacheModule>().AddImage(chunk.Content);
                        if(imageid > -1) {
                            BitmapImage image = emotecache[imageid];
                            if(image != null) {
                                paragraph.Inlines.Add(new Image {
                                    Source = image,
                                    Stretch = Stretch.Uniform,
                                    Height = 24.0
                                });
                            }
                        }
                    }
                    else {
                        paragraph.Inlines.Add(new Run(chunk.Content) {
                            FontWeight = TranslateWeight(chunk.FontWeight),
                            Foreground = new SolidColorBrush(TranslateColor(chunk.Color))
                        });
                    }
                }

                txtChat.Document.Blocks.Add(paragraph);
                paragraph.Loaded += paragraph_Loaded;
            }
        }

        void AddMessage(ChatMessage message) {
            lock(chatlock) {
                RemoveOldEntries();

                Paragraph paragraph = new Paragraph {
                    Margin = new Thickness(0.0),
                    Background = new SolidColorBrush(alternate ? Color.FromRgb(64, 64, 64) : Colors.Black)
                };
                alternate = !alternate;

                if(!string.IsNullOrEmpty(message.AvatarLink))
                    paragraph.Inlines.Add(new Image {
                        Source = emotecache[context.GetModule<ImageCacheModule>().AddImage(message.AvatarLink)],
                        Stretch = Stretch.Uniform,
                        Height = 24.0
                    });
                paragraph.Inlines.Add(new Run(message.User)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(FixColor(message.UserColor))
                });
                paragraph.Inlines.Add(new Run(": "));
                CreateInlines(message, paragraph);
                txtChat.Document.Blocks.Add(paragraph);
                paragraph.Loaded += paragraph_Loaded;
            }
        }

        void CreateInlines(ChatMessage message, Paragraph paragraph) {
            if(message.Emotes.Length == 0) {
                paragraph.Inlines.Add(new Run(message.Message));
                return;
            }

            int laststart = 0;
            foreach(ChatEmote emote in message.Emotes) {
                if(emote.StartIndex > laststart)
                    paragraph.Inlines.Add(new Run(message.Message.Substring(laststart, emote.StartIndex - laststart)));

                BitmapImage emoteimage = emotecache[emote.ImageID];
                if(emoteimage != null) {
                    paragraph.Inlines.Add(new Image {
                        Source = emoteimage,
                        Stretch = Stretch.Uniform,
                        Height = 24.0
                    });
                }
                laststart = emote.EndIndex + 1;
            }

            if(laststart < message.Message.Length)
                paragraph.Inlines.Add(new Run(message.Message.Substring(laststart)));
        }

        BitmapImage CreateImage(long id) {
            byte[] data = context.GetModule<ImageCacheModule>().GetImageData(id);

            if(data == null)
                return null;

            BitmapImage image = new BitmapImage();
                using(MemoryStream ms = new MemoryStream(data)) {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                }
            return image;
        }

        public override void Initialize() {
            base.Initialize();
            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Display.Status", (sender, args) => Show());
        }
    }
}
