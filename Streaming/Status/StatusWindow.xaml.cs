using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NightlyCode.Core.Collections.Cache;
using NightlyCode.Core.Conversion;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Core.TTS;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Chat;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Status
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    [Dependency(nameof(TTSModule))]
    [Dependency(nameof(MessageModule))]
    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(UserModule))]
    [Dependency(ModuleKeys.MainWindow, SpecifierType.Key)]
    [Dependency(nameof(ImageCacheModule))]
    [ModuleKey("status")]
    public partial class StatusWindow : ModuleWindow, IRunnableModule {
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
            TTSModule ttsmodule = context.GetModule<TTSModule>();

            if (IsVisible) {
                streammodule.ChatMessage += OnMessageReceived;
                streammodule.Hosted += OnHost;
                streammodule.Raid += OnRaid;
                streammodule.NewFollower += OnFollower;
                streammodule.NewSubscriber += OnSubscriber;
                streammodule.MicroPresent += OnMicroPresent;
                ttsmodule.TextSpoken += OnTextSpoken;
                context.GetModule<MessageModule>().Message += OnStatusMessage;
            }
            else {
                streammodule.ChatMessage -= OnMessageReceived;
                streammodule.Hosted -= OnHost;
                streammodule.Raid -= OnRaid;
                streammodule.NewFollower -= OnFollower;
                streammodule.NewSubscriber -= OnSubscriber;
                streammodule.MicroPresent -= OnMicroPresent;
                ttsmodule.TextSpoken -= OnTextSpoken;
                context.GetModule<MessageModule>().Message -= OnStatusMessage;
            }
        }

        void OnTextSpoken(string voice, string text) {
            Dispatcher.Invoke(() => AddStatus($"{voice}: {text}.", Colors.DarkRed));
        }

        void OnMicroPresent(MicroPresent present) {
            if(!string.IsNullOrEmpty(present.Message))
                Dispatcher.Invoke(() => AddStatus($"{present.Username} has donated {present.Amount} {present.Currency} -> \"{present.Message}\"", Colors.DarkBlue));
            else Dispatcher.Invoke(() => AddStatus($"{present.Username} has donated {present.Amount} {present.Currency}.", Colors.DarkBlue));
        }

        void OnStatusMessage(Message message) {
            Dispatcher.Invoke(() => AddMessage(message));
        }

        void OnSubscriber(SubscriberInformation subscriber) {
            Dispatcher.Invoke(() => AddStatus($"New Subscriber {subscriber.Username} ({subscriber.PlanName}).", Colors.DarkBlue));
        }

        void OnFollower(UserInformation follower) {
            Dispatcher.Invoke(() => AddStatus($"New Follower {follower.Username}.", Colors.DarkBlue));
        }

        void OnHost(HostInformation host) {
            Dispatcher.Invoke(() => AddStatus($"New Host from {host.Channel} for {host.Viewers} viewers.", Colors.DarkBlue));
        }

        void OnRaid(RaidInformation raid)
        {
            Dispatcher.Invoke(() => AddStatus($"New Host from {raid.Login} for {raid.RaiderCount} viewers.", Colors.DarkBlue));
        }

        void AddStatus(string message, Color backcolor) {
            lock(chatlock) {
                RemoveOldEntries();

                Paragraph paragraph = new Paragraph {
                    Background = new SolidColorBrush(backcolor),
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
            if(message.IsWhisper || message.Message.StartsWith("!"))
                return;

            Dispatcher.Invoke(() => AddMessage(message));
        }

        void RemoveOldEntries() {
            while(txtChat.Document.Blocks.Count > 64)
                txtChat.Document.Blocks.Remove(txtChat.Document.Blocks.FirstBlock);
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
                        long imageid;
                        if(!long.TryParse(chunk.Content, out imageid))
                            continue;

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
                            Foreground = new SolidColorBrush(TranslateColor(chunk.Color).FixColor())
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

                paragraph.Inlines.Add(new Image {
                    Source = emotecache[context.GetModule<ImageCacheModule>().AddImage($"http://localhost/streamrc/services/icon?service={message.Service}")],
                    Stretch = Stretch.Uniform,
                    Height = 24.0
                });

                if (!string.IsNullOrEmpty(message.AvatarLink))
                    paragraph.Inlines.Add(new Image {
                        Source = emotecache[context.GetModule<ImageCacheModule>().AddImage(message.AvatarLink)],
                        Stretch = Stretch.Uniform,
                        Height = 24.0
                    });

                paragraph.Inlines.Add(new Run(message.User)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(message.UserColor.FixColor())
                });
                paragraph.Inlines.Add(new Run(": "));
                CreateInlines(message, paragraph);
                txtChat.Document.Blocks.Add(paragraph);
                paragraph.Loaded += paragraph_Loaded;

                if(message.Attachements!=null)
                foreach(MessageAttachement attachement in message.Attachements) {
                    if(attachement.Type == AttachmentType.Image)
                        AddImage(attachement.URL, attachement.Width, attachement.Height);
                }
            }
        }

        void AddImage(string url, int width, int height) {
            lock(chatlock) {
                RemoveOldEntries();
                Paragraph paragraph = new Paragraph {
                    Margin = new Thickness(0.0),
                };

                if(width == 0) {
                    width = 320;
                    height = 180;
                }
                else {
                    if(width > height) {
                        float aspect = (float)height / width;
                        width = 320;
                        height = (int)(width * aspect);
                    }
                    else {
                        float aspect = (float)width / height;
                        height = 180;
                        width = (int)(height * aspect);
                    }
                }

                long imageid = context.GetModule<ImageCacheModule>().ExtractIDFromUrl(url);
                if(imageid == -1)
                    imageid = context.GetModule<ImageCacheModule>().AddImage(url, DateTime.Now + TimeSpan.FromMinutes(5.0));

                paragraph.Inlines.Add(new Image {
                    Source = emotecache[imageid],
                    Stretch = Stretch.Uniform,
                    Width = width,
                    Height = height
                });

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

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().ViewersChanged += OnViewersChanged;
        }

        void OnViewersChanged(int viewers) {
            Dispatcher.Invoke(() => {
                lblViewers.Content = viewers.ToString();
            });
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().ViewersChanged += OnViewersChanged;
        }
    }
}
