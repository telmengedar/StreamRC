﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Data;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.RPG.Players {

    /// <summary>
    /// module managing player level table
    /// </summary>
    public class PlayerLevelModule : IInitializableModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="PlayerLevelModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public PlayerLevelModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// get level data for a player
        /// </summary>
        /// <param name="level">level for which to get level data</param>
        /// <returns></returns>
        public LevelEntry GetLevelData(int level) {
            return context.Database.LoadEntities<LevelEntry>().Where(l => l.Level == level).Execute().FirstOrDefault();
        }

        /// <summary>
        /// get all known level entries
        /// </summary>
        /// <returns>level data</returns>
        public IEnumerable<LevelEntry> GetLevelEntries() {
            return context.Database.LoadEntities<LevelEntry>().Execute();
        }

        /// <summary>
        /// initializes the <see cref="T:NightlyCode.Modules.IModule"/> to prepare for start
        /// </summary>
        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<LevelEntry>();
            foreach(LevelEntry entry in DataTable.ReadCSV(ResourceAccessor.GetResource<Stream>($"{GetType().Namespace}.leveltable.csv"), '\t', true).Deserialize<LevelEntry>()) {
                if(context.Database.Update<LevelEntry>().Set(e => e.Experience == entry.Experience, e => e.Health == entry.Health, e => e.Mana == entry.Mana, e => e.Stamina == entry.Stamina, e => e.Strength == entry.Strength, e => e.Dexterity == entry.Dexterity, e => e.Fitness == entry.Fitness, e => e.Luck == entry.Luck, e=>e.Intelligence==entry.Intelligence).Where(e => e.Level == entry.Level).Execute() == 0)
                    context.Database.Insert<LevelEntry>().Columns(e => e.Level, e => e.Experience, e => e.Health, e => e.Mana, e => e.Stamina, e => e.Strength, e => e.Dexterity, e => e.Fitness, e => e.Luck, e=>e.Intelligence).Values(entry.Level, entry.Experience, entry.Health, entry.Mana, entry.Stamina, entry.Strength, entry.Dexterity, entry.Fitness, entry.Luck, entry.Intelligence).Execute();
            }
        }
    }
}