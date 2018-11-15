using System;
using System.Collections.Generic;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Core.Settings;
using StreamRC.Core.Timer;

namespace StreamRC.Streaming.Ticker {

    /// <summary>
    /// manages ticker messages
    /// </summary>
    [Module(Key="ticker")]
    public class TickerModule : ITimerService {
        readonly ISettings settings;
        readonly TimerModule timer;

        readonly object sourcelock = new object();
        readonly List<ITickerMessageSource> sources=new List<ITickerMessageSource>();
        TimeSpan interval;

        int index = -1;

        /// <summary>
        /// creates a new <see cref="TickerModule"/>
        /// </summary>
        /// <param name="settings">access to configuration</param>
        public TickerModule(ISettings settings, TimerModule timer) {
            this.settings = settings;
            this.timer = timer;
            interval = settings.Get(this, "Interval", TimeSpan.FromMinutes(5.0));
            timer.AddService(this, Interval.TotalSeconds);
        }

        /// <summary>
        /// interval in which ticker messages are displayed
        /// </summary>
        public TimeSpan Interval
        {
            get => interval;
            set
            {
                if(interval == value)
                    return;
                interval = value;
                timer.ChangeInterval(this, interval.TotalSeconds);
                settings.Set(this, "Interval", value);
            }
        }

        /// <summary>
        /// triggered when a new message was generated
        /// </summary>
        public event Action<TickerMessage> Message;

        /// <summary>
        /// adds a source used when generating ticker messages
        /// </summary>
        /// <param name="source">source to be added</param>
        public void AddSource(ITickerMessageSource source) {
            lock(sourcelock)
                sources.Add(source);
        }

        /// <summary>
        /// removes a source
        /// </summary>
        /// <param name="source">source to be removed</param>
        public void RemoveSource(ITickerMessageSource source) {
            lock(sourcelock)
                sources.Remove(source);
        }

        void ITimerService.Process(double time) {
            lock(sourcelock) {
                if(sources.Count == 0)
                    return;

                index = (index + 1) % sources.Count;

                try {
                    TickerMessage message = sources[index].GenerateTickerMessage();
                    if(message != null)
                        Message?.Invoke(message);
                }
                catch(Exception e) {
                    Logger.Error(this, "Error displaying ticker message", e);
                }
                
            }
        }

        public void Show(string message) {
            Message?.Invoke(new TickerMessage
            {
                Content = Core.Messages.Message.Parse(message)
            });
        }
    }
}