using System;
using System.Collections.Generic;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Ticker {

    /// <summary>
    /// manages ticker messages
    /// </summary>
    [Dependency(nameof(TimerModule))]
    [ModuleKey("ticker")]
    public class TickerModule : IInitializableModule, IRunnableModule, ITimerService, ICommandModule {
        readonly Context context;

        readonly object sourcelock = new object();
        readonly List<ITickerMessageSource> sources=new List<ITickerMessageSource>();
        TimeSpan interval;

        int index = -1;

        /// <summary>
        /// creates a new <see cref="TickerModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public TickerModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// interval in which ticker messages are displayed
        /// </summary>
        public TimeSpan Interval
        {
            get { return interval; }
            set
            {
                if(interval == value)
                    return;
                interval = value;
                context.GetModule<TimerModule>().ChangeInterval(this, interval.TotalSeconds);
                context.Settings.Set(this, "Interval", value);
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

        void IInitializableModule.Initialize() {
            interval = context.Settings.Get(this, "Interval", TimeSpan.FromMinutes(5.0));
        }

        void IRunnableModule.Start() {
            context.GetModule<TimerModule>().AddService(this, Interval.TotalSeconds);
        }

        void IRunnableModule.Stop() {
            context.GetModule<TimerModule>().RemoveService(this);
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

        public void ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "show":
                    Message?.Invoke(new TickerMessage {
                        Content = Core.Messages.Message.Parse(arguments[0])
                    });
                    break;
                case "interval":
                    Interval = TimeSpan.Parse(arguments[0]);
                    break;
                default:
                    throw new StreamCommandException($"'{command}' not implemented by this module");
            }
        }
    }
}