using NightlyCode.DB.Clients;
using NightlyCode.DB.Entities;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.RPG.Data {

    /// <summary>
    /// module containing database for rpg runtime data
    /// </summary>
    public class RPGDatabaseModule : IModule {
        Context context;
        readonly IEntityManager database = new EntityManager(DBClient.CreateSQLite(null));

        public RPGDatabaseModule(Context context) {
            this.context = context;
        }

        public IEntityManager Database => database;
    }
}