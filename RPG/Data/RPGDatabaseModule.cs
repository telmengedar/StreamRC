using NightlyCode.Database.Entities;
using NightlyCode.Modules;
using StreamRC.Core;

namespace StreamRC.RPG.Data {

    /// <summary>
    /// module containing database for rpg runtime data
    /// </summary>
    [Module]
    public class RPGDatabaseModule {

        public RPGDatabaseModule(DatabaseModule databasemodule) {
            Database = databasemodule.CreateMemoryDatabase();
        }

        public IEntityManager Database { get; }
    }
}