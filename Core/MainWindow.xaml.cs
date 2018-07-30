using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using NightlyCode.Core.Helpers;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ModuleKey(ModuleKeys.MainWindow)]
    public partial class MainWindow : Window, IInitializableModule, IRunnableModule, IMainWindow {
        readonly Context context;
        IChatMessageSender chat;

        readonly List<string> commandhistory=new List<string>();
        int index = -1;

        /// <summary>
        /// creates a new <see cref="MainWindow"/>
        /// </summary>
        public MainWindow(Context context) {
            this.context = context;
            InitializeComponent();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            context.Settings.Set(this, "x", Left);
            context.Settings.Set(this, "y", Top);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.WidthChanged)
                context.Settings.Set(this, "width", sizeInfo.NewSize.Width);
            if (sizeInfo.HeightChanged)
                context.Settings.Set(this, "height", sizeInfo.NewSize.Height);
        }

        protected void LoadSettings()
        {
            double width = context.Settings.Get(this, "width", Width);
            double height = context.Settings.Get(this, "height", Height);
            double x = context.Settings.Get(this, "x", 0.0);
            double y = context.Settings.Get(this, "y", 0.0);
            Width = width;
            Height = height;
            Left = x;
            Top = y;
        }

        /// <summary>
        /// adds an item to the menu bar
        /// </summary>
        /// <param name="menuname">name of menu</param>
        /// <param name="action">action to be executed when item is clicked</param>
        public void AddMenuItem(string menuname, RoutedEventHandler action) {
            string[] path = menuname.Split('.');

            ItemCollection currentcollection = menu.Items;
            for(int i = 0; i < path.Length - 1; ++i) {
                MenuItem menui = null;
                for (int k = 0; k < currentcollection.Count; ++k)
                    if ((currentcollection[k] as MenuItem)?.Header as string == path[i])
                    {
                        menui = currentcollection[k] as MenuItem;
                        break;
                    }

                if (menui == null)
                {
                    menui = new MenuItem
                    {
                        Header = path[i]
                    };
                    currentcollection.Add(menui);
                }

                currentcollection = menui.Items;
            }


            MenuItem item = new MenuItem {
                Header = path[path.Length-1]
            };
            item.Click += action;
            currentcollection.Add(item);
        }

        public void AddSeparator(string menuname) {
            string[] path = menuname.Split('.');

            ItemCollection currentcollection = menu.Items;
            for (int i = 0; i < path.Length; ++i)
            {
                MenuItem menui = null;
                for (int k = 0; k < currentcollection.Count; ++k)
                    if ((currentcollection[k] as MenuItem)?.Header as string == path[i])
                    {
                        menui = currentcollection[k] as MenuItem;
                        break;
                    }

                if (menui == null)
                {
                    menui = new MenuItem
                    {
                        Header = path[i]
                    };
                    currentcollection.Add(menui);
                }

                currentcollection = menui.Items;
            }

            currentcollection.Add(new Separator());
        }

        void OnLogMessage(MessageType type, string sender, string message, string details)
        {
            Color messagecolor;
            Color detailcolor;

            switch (type)
            {
                case MessageType.Info:
                    messagecolor = Colors.Black;
                    detailcolor = Colors.Gray;
                    break;
                case MessageType.Warning:
                    messagecolor = Color.FromRgb(163, 124, 0);
                    detailcolor = Color.FromRgb(204, 166, 80);
                    break;
                case MessageType.Error:
                    messagecolor = Colors.DarkRed;
                    detailcolor = Colors.IndianRed;
                    break;
                default:
                    return;
            }

            Dispatcher.BeginInvoke((Action)(() => {
                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run($"{sender}: {message}")
                {
                    Foreground = new SolidColorBrush(messagecolor)
                });
                if (!string.IsNullOrEmpty(details))
                {
                    paragraph.Inlines.Add(new LineBreak());
                    paragraph.Inlines.Add(new Run(details)
                    {
                        Foreground = new SolidColorBrush(detailcolor)
                    });
                }

                paragraph.Loaded += OnParagraphLoaded;
                log.Blocks.Add(paragraph);
            }));
        }

        void OnParagraphLoaded(object sender, RoutedEventArgs e)
        {
            Paragraph paragraph = (Paragraph)sender;
            paragraph.Loaded -= OnParagraphLoaded;
            paragraph.BringIntoView();
        }

        void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter || e.Key == Key.Return) {
                string message = txtMessage.Text;
                if(message.StartsWith("$")) {
                    if(message.Length > 2 && message[1] == '$') {
                        try {
                            context.ExecuteCommand(message.Substring(2));
                            Logger.Info(this, "Command executed");
                        }
                        catch(Exception ex) {
                            Logger.Error(this, "Error executing command", ex);
                        }
                    }
                    else {
                        string[] arguments = Commands.SplitArguments(message.Substring(1)).ToArray();
                        switch(arguments[0]) {
                            default:
                                try {
                                    context.ExecuteCommand(arguments[0], arguments[1], arguments.Skip(2).ToArray());
                                }
                                catch(Exception ex) {
                                    Logger.Error(this, "Error executing command", ex);
                                }
                                break;
                        }
                    }
                }
                else {
                    chat?.SendChatMessage(message);
                }

                AddToHistory(message);
                
                txtMessage.Text = "";
                e.Handled = true;
            }
            else if(e.Key == Key.PageUp) {
                if(commandhistory.Count == 0)
                    return;

                index = (index - 1 + commandhistory.Count) % commandhistory.Count;
                txtMessage.Text = commandhistory[index];
            }
            else if(e.Key == Key.PageDown) {
                if (commandhistory.Count == 0)
                    return;

                index = (index + 1 + commandhistory.Count) % commandhistory.Count;
                txtMessage.Text = commandhistory[index];
            }
        }

        void AddToHistory(string message) {
            commandhistory.Insert(0, message);
            for(int i = commandhistory.Count - 1; i >= 0 && i > 32; --i)
                commandhistory.RemoveAt(i);
            index = -1;
        }

        void IInitializableModule.Initialize() {
            LoadSettings();
            Logger.Message += OnLogMessage;
        }

        void IRunnableModule.Start() {
            Show();
            chat = context.Modules.FirstOrDefault(m => m.Module is IChatMessageSender)?.Module as IChatMessageSender;
        }

        void IRunnableModule.Stop() {
        }
    }
}
