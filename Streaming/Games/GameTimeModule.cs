using System;
using NightlyCode.Modules;
using StreamRC.Streaming.Statistics;

namespace StreamRC.Streaming.Games {

    [Module(Key="gametime")]
    public class GameTimeModule : IDisposable {
        readonly StatisticModule statistics;
        bool running = false;
        DateTime start = DateTime.Now;

        public GameTimeModule(StatisticModule statistics) {
            this.statistics = statistics;
        }

        public void StartGame() {
            start = DateTime.Now;
            if(!statistics.Exists("Game Time"))
                statistics.Set("Game Time", 0);
            running = true;
        }

        public TimeSpan GetTime() {
            if(running)
                return DateTime.Now - start;
            return TimeSpan.Zero;
        }

        void IDisposable.Dispose() {
            if(running)
                statistics.Increase("Game Time", GetTime());
            running = false;
        }
    }
}