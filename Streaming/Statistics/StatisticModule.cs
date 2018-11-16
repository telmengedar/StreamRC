using System;
using System.Collections.Generic;
using NightlyCode.Core.Logs;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Modules;
using StreamRC.Core;

namespace StreamRC.Streaming.Statistics {

    [Module(Key="statistics")]
    public class StatisticModule {
        readonly DatabaseModule database;

        public StatisticModule(DatabaseModule database) {
            this.database = database;
            database.Database.UpdateSchema<Statistic>();
        }

        /// <summary>
        /// clears all statistics
        /// </summary>
        public void Clear() {
            database.Database.Delete<Statistic>().Execute();
        }

        /// <summary>
        /// resets a statistic value
        /// </summary>
        /// <param name="name">name of statistic</param>
        public void Reset(string name) {
            database.Database.Delete<Statistic>().Where(s => s.Name == name).Execute();
        }

        public void Increase(string name) {
            if(database.Database.Update<Statistic>().Set(s => s.Value == s.Value + 1).Where(s => s.Name == name).Execute() == 0)
                database.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, 1).Execute();
            Logger.Info(this, $"{name} increased");
        }

        public void Increase(string name, TimeSpan time) {
            if (database.Database.Update<Statistic>().Set(s => s.Value == s.Value + time.Ticks).Where(s => s.Name == name).Execute() == 0)
                database.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, time.Ticks).Execute();
            Logger.Info(this, $"{name} increased by {time}");
        }

        public void Decrease(string name) {
            if (database.Database.Update<Statistic>().Set(s => s.Value == s.Value - 1).Where(s => s.Name == name).Execute() == 0)
                database.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, -1).Execute();
            Logger.Info(this, $"{name} decreased");
        }

        public void Set(string name, long value) {
            if (database.Database.Update<Statistic>().Set(s => s.Value == value).Where(s => s.Name == name).Execute() == 0)
                database.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, value).Execute();
            Logger.Info(this, $"{name} set to {value}");
        }

        public void Set(string name, TimeSpan time) {
            if (database.Database.Update<Statistic>().Set(s => s.Value == time.Ticks).Where(s => s.Name == name).Execute() == 0)
                database.Database.Insert<Statistic>().Columns(s => s.Name, s => s.Value).Values(name, time.Ticks).Execute();
            Logger.Info(this, $"{name} set to {time}");
        }

        public IEnumerable<Statistic> Get() {
            return database.Database.LoadEntities<Statistic>().Execute();
        }

        public bool Exists(string name) {
            return database.Database.Load<Statistic>(c => DBFunction.Count).Where(s => s.Name == name).ExecuteScalar<int>() > 0;
        }
    }
}