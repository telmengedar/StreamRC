using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Core.Scripts;
using StreamRC.Core.Settings;
using StreamRC.Core.UI;

namespace StreamRC.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Module(Key=ModuleKeys.MainWindow)]
    public partial class MainWindow : ModuleWindow, IMainWindow {
        readonly IChatMessageSender chat;
        readonly ScriptModule scripts;

        readonly List<string> commandhistory=new List<string>();
        int index = -1;
        
        /// <summary>
        /// creates a new <see cref="MainWindow"/>
        /// </summary>
        public MainWindow(ISettings settings, IChatMessageSender chat, ScriptModule scripts)
            : base(settings)
        {
            this.chat = chat;
            this.scripts = scripts;
            InitializeComponent();
            Logger.Message += OnLogMessage;
            Show();
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
                foreach(object item in currentcollection)
                    if ((item as MenuItem)?.Header as string == path[i])
                    {
                        menui = item as MenuItem;
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
                while(log.Blocks.Count > 256)
                    log.Blocks.Remove(log.Blocks.FirstBlock);

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
                    try {
                        object result = scripts.Execute(message.Substring(1))??"Executed";
                        if (result is Array array)
                            result = string.Join("\n", array.Cast<object>());
                        Logger.Info(this, message.Substring(1), $"{result}");
                    }
                    catch (Exception ex) {
                        Logger.Error(this, "Error executing command", ex);
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
    }
}
