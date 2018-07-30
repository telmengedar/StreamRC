using System;
using System.Collections.Generic;
using NightlyCode.Core.Logs;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Streaming.Statistics {

    [ModuleKey("statistics")]
    public class StatisticModule : IInitializableModule {
        readonly Context context;

        public StatisticModule(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            context.Database.Create<Statistic>();
        }

        /// <summary>
        /// clears all statistics
        /// </summary>
        public void Clear() {
            context.Database.Delete<Statistic>().Execute();
        }

        /// <summary>
        /// resets a statistic value
        /// </summary>
        /// <param name="name">name of statistic</param>
        public void Reset(string name) {
            context.Database.Delete<Statistic>().Where(s => s.Name == name).Execute();
        }

        public void Increase(string name) {
            if(context.Database.Update<Statistic>().Set(s => s.Value == s.Value + 1).Where(s => s.Name == name).Execute() == 0)
                context.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, 1).Execute();
            Logger.Info(this, $"{name} increased");
        }

        public void Increase(string name, TimeSpan time) {
            if (context.Database.Update<Statistic>().Set(s => s.Value == s.Value + time.Ticks).Where(s => s.Name == name).Execute() == 0)
                context.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, time.Ticks).Execute();
            Logger.Info(this, $"{name} increased by {time}");
        }

        public void Decrease(string name) {
            if (context.Database.Update<Statistic>().Set(s => s.Value == s.Value - 1).Where(s => s.Name == name).Execute() == 0)
                context.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, -1).Execute();
            Logger.Info(this, $"{name} decreased");
        }

        public void Set(string name, long value) {
            if (context.Database.Update<Statistic>().Set(s => s.Value == value).Where(s => s.Name == name).Execute() == 0)
                context.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, value).Execute();
            Logger.Info(this, $"{name} set to {value}");
        }

        public void Set(string name, TimeSpan time) {
            if (context.Database.Update<Statistic>().Set(s => s.Value == time.Ticks).Where(s => s.Name == name).Execute() == 0)
                context.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, time.Ticks).Execute();
            Logger.Info(this, $"{name} set to {time}");
        }

        public IEnumerable<Statistic> Get() {
            return context.Database.LoadEntities<Statistic>().Execute();
        }

        public bool Exists(string name) {
            return context.Database.Load<Statistic>(DBFunction.Count).Where(s => s.Name == name).ExecuteScalar<int>() > 0;
        }
    }
}