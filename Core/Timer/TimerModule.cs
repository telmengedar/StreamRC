using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Threading;
using NightlyCode.Modules;
using StreamRC.Core.Settings;

namespace StreamRC.Core.Timer {
    /// <summary>
    /// module providing a periodic timer
    /// </summary>
    [Module(AutoCreate = true)]
    public class TimerModule : ITimerModule {
        readonly ISettings settings;

        readonly PeriodicTimer timer=new PeriodicTimer();
        DateTime lastexecution = DateTime.Now;

        readonly object timerlock = new object();
        readonly object lateaddlock = new object();

        readonly HashSet<ITimerService> services=new HashSet<ITimerService>();
        TimeSpan resolution;

        readonly List<TimerEntry> intervaltable=new List<TimerEntry>();
        readonly List<TimerEntry> lateadd = new List<TimerEntry>();

        bool processing;

        /// <summary>
        /// creates a new <see cref="TimerModule"/>
        /// </summary>
        /// <param name="settings">access to configuration</param>
        public TimerModule(ISettings settings) {
            this.settings = settings;
            timer.Elapsed += OnTimer;
            Resolution = settings.Get(this, "Resolution", TimeSpan.FromSeconds(0.5));
            timer.Start(Resolution);
        }

        void OnTimer() {
            double time = (DateTime.Now - lastexecution).TotalSeconds;
            lastexecution = DateTime.Now;

            processing = true;
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

                lock (lateaddlock) {
                    if (lateadd.Count > 0) {
                        foreach (TimerEntry entry in lateadd)
                            intervaltable.Add(entry);
                        lateadd.Clear();
                    }
                }

                processing = false;
            }
        }

        /// <summary>
        /// resolution of timer
        /// </summary>
        public TimeSpan Resolution
        {
            get => resolution;
            set
            {
                if(resolution == value)
                    return;

                resolution = value;
                settings.Set(this, "Resolution", resolution);
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

            if (processing) {
                lock (lateaddlock) {
                    lateadd.Add(new TimerEntry(service, interval));
                }
            }

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
    }
}