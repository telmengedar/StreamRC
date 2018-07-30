using System;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Streaming.Statistics;

namespace StreamRC.Streaming.Games {

    [Dependency(nameof(StatisticModule), SpecifierType.Type)]
    [ModuleKey("gametime")]
    public class GameTimeModule : IModule, IRunnableModule {
        readonly Context context;

        bool running = false;
        DateTime start = DateTime.Now;

        public GameTimeModule(Context context) {
            this.context = context;
        }

        public void StartGame() {
            start = DateTime.Now;
            if(!context.GetModule<StatisticModule>().Exists("Game Time"))
                context.GetModule<StatisticModule>().Set("Game Time", 0);
            running = true;
        }

        public TimeSpan GetTime() {
            if(running)
                return DateTime.Now - start;
            return TimeSpan.Zero;
        }

        void IRunnableModule.Start() {
        }

        void IRunnableModule.Stop() {
            if(running)
                context.GetModule<StatisticModule>().Increase("Game Time", GetTime());
            running = false;
        }
    }
}