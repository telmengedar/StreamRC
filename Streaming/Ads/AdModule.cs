using System;
using NightlyCode.Core.Collections;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Ads {

    /// <summary>
    /// module used to display ads in chat
    /// </summary>
    [ModuleKey("ads")]
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(TimerModule), DependencyType.Type)]
    public class AdModule : IInitializableModule, ICommandModule, IRunnableModule, ITimerService {
        readonly Context context;

        readonly CircularList<Ad> ads = new CircularList<Ad>();
        TimeSpan interval;

        /// <summary>
        /// creates a new <see cref="AdModule"/>
        /// </summary>
        /// <param name="context">module context</param>
        public AdModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// interval used to display ads
        /// </summary>
        public TimeSpan Interval
        {
            get { return interval; }
            set
            {
                if(interval == value)
                    return;
                interval = value;
                context.Settings.Set(this, "Interval", value);
            }
        }

        void ReloadAds() {
            ads.Clear();
            ads.AddRange(context.Database.LoadEntities<Ad>().Where(a=>a.Active).Execute());
            Logger.Info(this, "Reloaded ads");
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<Ad>();

            interval = context.Settings.Get<TimeSpan>(this, "Interval", TimeSpan.FromMinutes(15.0));
            ReloadAds();
        }

        void ICommandModule.ProcessCommand(string command, string[] arguments) {
            switch(command) {
                case "set":
                    SetAdText(arguments[0], arguments[1]);
                    break;
                case "remove":
                    RemoveAd(arguments[0]);
                    break;
                case "enable":
                    context.Database.Update<Ad>().Set(a => a.Active == true).Where(a => a.Key == arguments[0]).Execute();
                    ReloadAds();
                    break;
                case "disable":
                    context.Database.Update<Ad>().Set(a => a.Active == false).Where(a => a.Key == arguments[0]).Execute();
                    ReloadAds();
                    break;
                default:
                    throw new Exception($"Unknown command '{command}' not supported");
            }
        }

        /// <summary>
        /// sets the text of an ad
        /// </summary>
        /// <param name="key">key of ad</param>
        /// <param name="text">ad text</param>
        public void SetAdText(string key, string text) {
            if (context.Database.Update<Ad>().Set(a => a.Text == text).Where(a => a.Key == key).Execute() == 0)
                context.Database.Insert<Ad>().Columns(a => a.Key, a => a.Text).Values(key, text).Execute();
            ReloadAds();
            Logger.Info(this, $"ad '{key}' changed", text);
        }

        /// <summary>
        /// removes an ad text
        /// </summary>
        /// <param name="key">key of ad</param>
        public void RemoveAd(string key) {
            context.Database.Delete<Ad>().Where(a => a.Key == key).Execute();
            ReloadAds();
            Logger.Info(this, $"ad '{key}' removed");
        }

        void IRunnableModule.Start() {
            context.GetModule<TimerModule>().AddService(this, Interval.TotalSeconds);
        }

        void IRunnableModule.Stop() {
            context.GetModule<TimerModule>().RemoveService(this);
        }

        void ITimerService.Process(double time) {
            Ad ad = ads.NextItem;
            if(ad != null)
                context.GetModule<StreamModule>().BroadcastMessage(ad.Text);
        }
    }
}