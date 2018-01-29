using NightlyCode.DB.Entities;
using NightlyCode.DB.Entities.Operations;
using StreamRC.RPG.Exploration.Data;

namespace StreamRC.RPG.Exploration.Statements {
    public class AdjectiveSource : ITermSource {
        readonly IEntityManager database;

        public AdjectiveSource(IEntityManager database) {
            this.database = database;
        }

        public ITerm GetTerm() {
            return new Term(database.Load<Adjective>(a => a.Name).OrderBy(DBFunction.Random).Limit(1).ExecuteScalar<string>());
        }
    }
}