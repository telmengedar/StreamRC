using System.Data.SQLite;
using NightlyCode.DB.Clients;
using NightlyCode.DB.Entities;
using NightlyCode.DB.Info;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.RPG.Data {

    /// <summary>
    /// module containing database for rpg runtime data
    /// </summary>
    public class RPGDatabaseModule : IModule {
        Context context;
        readonly IEntityManager database = new EntityManager(new DBClient(new SQLiteConnection("Data Source=:memory:"), new SQLiteInfo()));

        public RPGDatabaseModule(Context context) {
            this.context = context;
        }

        public IEntityManager Database => database;
    }
}