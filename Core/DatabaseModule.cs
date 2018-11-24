using Microsoft.Data.Sqlite;
using NightlyCode.Database.Clients;
using NightlyCode.Database.Entities;
using NightlyCode.Database.Info;
using NightlyCode.Modules;

namespace StreamRC.Core {

    /// <summary>
    /// module providing access to database
    /// </summary>
    [Module]
    public class DatabaseModule : IDatabaseModule {

        /// <summary>
        /// database manager
        /// </summary>
        public IEntityManager Database { get; } = new EntityManager(new DBClient(new SqliteConnection("Data Source=streamrc.db3"), new SQLiteInfo()));

        /// <summary>
        /// creates an new in-memory database
        /// </summary>
        /// <returns>entity manager to use to access database</returns>
        public IEntityManager CreateMemoryDatabase() {
            return new EntityManager(new DBClient(new SqliteConnection("Data Source=:memory:"), new SQLiteInfo()));
        }
    }
}