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
    public class DatabaseModule {
        readonly IEntityManager entitymanager = new EntityManager(new DBClient(new SqliteConnection("Data Source=streamrc.db3"), new SQLiteInfo()));

        /// <summary>
        /// database manager
        /// </summary>
        public IEntityManager Database => entitymanager;
    }
}