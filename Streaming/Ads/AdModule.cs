using System;
using NightlyCode.Core.Collections;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Settings;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Ads {

    /// <summary>
    /// module used to display ads in chat
    /// </summary>
    [Module(Key="ads", AutoCreate = true)]
    public class AdModule : ITimerService {
        readonly DatabaseModule database;
        readonly ISettings settings;
        readonly UserModule usermodule;
        readonly StreamModule stream;

        readonly CircularList<Ad> ads = new CircularList<Ad>();
        TimeSpan interval;

        /// <summary>
        /// creates a new <see cref="AdModule"/>
        /// </summary>
        /// <param name="database">access to database</param>
        public AdModule(DatabaseModule database, ISettings settings, TimerModule timer, UserModule usermodule, StreamModule stream) {
            this.database = database;
            this.settings = settings;
            this.usermodule = usermodule;
            this.stream = stream;
            database.Database.UpdateSchema<Ad>();

            interval = settings.Get(this, "Interval", TimeSpan.FromMinutes(15.0));
            ReloadAds();

            timer.AddService(this, Interval.TotalSeconds);
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
                settings.Set(this, "Interval", value);
            }
        }

        void ReloadAds() {
            ads.Clear();
            ads.AddRange(database.Database.LoadEntities<Ad>().Where(a=>a.Active).Execute());
            Logger.Info(this, "Reloaded ads");
        }

        public void Enable(string ad) {
            database.Database.Update<Ad>().Set(a => a.Active == true).Where(a => a.Key == ad).Execute();
            ReloadAds();
        }

        public void Disable(string ad) {
            database.Database.Update<Ad>().Set(a => a.Active == false).Where(a => a.Key == ad).Execute();
            ReloadAds();
        }

        /// <summary>
        /// sets the text of an ad
        /// </summary>
        /// <param name="key">key of ad</param>
        /// <param name="text">ad text</param>
        public void SetAdText(string key, string text) {
            if (database.Database.Update<Ad>().Set(a => a.Text == text).Where(a => a.Key == key).Execute() == 0)
                database.Database.Insert<Ad>().Columns(a => a.Key, a => a.Text).Values(key, text).Execute();
            ReloadAds();
            Logger.Info(this, $"ad '{key}' changed", text);
        }

        /// <summary>
        /// removes an ad text
        /// </summary>
        /// <param name="key">key of ad</param>
        public void RemoveAd(string key) {
            database.Database.Delete<Ad>().Where(a => a.Key == key).Execute();
            ReloadAds();
            Logger.Info(this, $"ad '{key}' removed");
        }

        void ITimerService.Process(double time) {
            if(usermodule.ActiveUserCount == 0)
                return;

            Ad ad = ads.NextItem;
            if(ad != null)
                stream.GetChannels(ChannelFlags.Notification).Foreach(c => c.SendMessage(ad.Text));
        }
    }
}