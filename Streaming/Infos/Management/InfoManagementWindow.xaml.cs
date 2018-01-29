using System.Windows;
using NightlyCode.Core.Collections;

namespace StreamRC.Streaming.Infos.Management
{
    /// <summary>
    /// Interaction logic for InfoManagementWindow.xaml
    /// </summary>
    public partial class InfoManagementWindow : Window
    {
        readonly InfoModule module;
        readonly NotificationList<InfoItem> infos=new NotificationList<InfoItem>();

        /// <summary>
        /// creates a new <see cref="InfoManagementWindow"/>
        /// </summary>
        /// <param name="module">module used to access infos</param>
        public InfoManagementWindow(InfoModule module)
        {
            InitializeComponent();
            this.module = module;

            foreach(Info info in module.GetInfos())
                infos.Add(new InfoItem(info));

            grdInfos.ItemsSource = infos;
            infos.ItemChanged += OnListItemChanged;
            infos.ItemRemoved += OnItemRemoved;
        }

        void OnItemRemoved(InfoItem item) {
            module.RemoveInfo(item.Key);
        }

        void OnListItemChanged(InfoItem info, string property) {
            if(string.IsNullOrEmpty(info.Key))
                return;

            switch(property) {
                case "Key":
                    if(string.IsNullOrEmpty(info.OldKey))
                        module.SetInfo(info.Key, info.Text);
                    else module.ChangeInfo(info.OldKey, info.Key, info.Text);
                    break;
                default:
                    module.SetInfo(info.Key, info.Text);
                    break;
            }
            info.Apply();
        }
    }
}
