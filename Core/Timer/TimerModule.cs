using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Threading;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Core.Timer {

    /// <summary>
    /// module providing a periodic timer
    /// </summary>
    public class TimerModule : IRunnableModule, IInitializableModule {
        readonly Context context;

        readonly PeriodicTimer timer=new PeriodicTimer();
        DateTime lastexecution = DateTime.Now;

        readonly object timerlock = new object();
        readonly HashSet<ITimerService> services=new HashSet<ITimerService>();
        TimeSpan resolution;

        readonly List<TimerEntry> intervaltable=new List<TimerEntry>();
         
        /// <summary>
        /// creates a new <see cref="TimerModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public TimerModule(Context context) {
            this.context = context;
            timer.Elapsed += OnTimer;
        }

        void OnTimer() {
            double time = (DateTime.Now - lastexecution).TotalSeconds;
            lastexecution = DateTime.Now;
            lock (timerlock) {
                foreach(ITimerService service in services) {
                    try {
                        service.Process(time);
                    }
                    catch(Exception e) {
                        Logger.Error(this, $"Error executing '{service.GetType().Name}'", e);
                    }
                }

                foreach(TimerEntry entry in intervaltable) {
                    if((entry.Time += time) >= entry.Period) {
                        try {
                            entry.Service.Process(entry.Time);
                        }
                        catch(Exception e) {
                            Logger.Error(this, $"Error executing '{entry.Service.GetType().Name}'", e);
                        }
                        
                        entry.Time = 0.0;
                    }
                }
            }
            
        }

        /// <summary>
        /// resolution of timer
        /// </summary>
        public TimeSpan Resolution
        {
            get { return resolution; }
            set
            {
                if(resolution == value)
                    return;

                resolution = value;
                context.Settings.Set(this, "Resolution", resolution);
            }
        }

        /// <summary>
        /// adds a service to the timer
        /// </summary>
        /// <remarks>
        /// if interval is not specified the service is executed as often as possible (every frame, step)
        /// </remarks>
        /// <param name="service">service to be executed regularly</param>
        /// <param name="interval">interval in which to execute service (optional)</param>
        public void AddService(ITimerService service, double interval=0.0) {
            lock(timerlock) {
                if(interval > 0.0)
                    intervaltable.Add(new TimerEntry(service, interval));
                else services.Add(service);
            }
        }

        /// <summary>
        /// changes the interval for a registered service
        /// </summary>
        /// <param name="service">service for which to change interval</param>
        /// <param name="interval">interval at which service is to be triggered</param>
        public void ChangeInterval(ITimerService service, double interval = 0.0) {
            lock(timerlock) {
                TimerEntry entry = intervaltable.FirstOrDefault(e => e.Service == service);
                if(entry != null)
                    entry.Period = interval;
            }
        }

        /// <summary>
        /// removes a service from the timer
        /// </summary>
        /// <param name="service">service not to be executed regularly anymore</param>
        public void RemoveService(ITimerService service) {
            lock(timerlock) {
                services.Remove(service);
                intervaltable.RemoveAll(e => e.Service == service);
            }
        }

        void IRunnableModule.Start() {
            timer.Start(Resolution);
        }

        void IRunnableModule.Stop() {
            timer.Stop();
        }

        void IInitializableModule.Initialize() {
            Resolution = context.Settings.Get(this, "Resolution", TimeSpan.FromSeconds(0.5));
        }
    }
}