using System.Linq;
using NightlyCode.DB.Entities;
using NightlyCode.DB.Entities.Operations;

namespace StreamRC.RPG.Exploration.Statements {
    public class NounSource : ITermSource {
        readonly IEntityManager database;

        public NounSource(IEntityManager database) {
            this.database = database;
        }

        public ITerm GetTerm() {
            return new NounTerm(database.LoadEntities<Noun>().OrderBy(new OrderByCriteria(DBFunction.Random)).Limit(1).Execute().FirstOrDefault());
        }
    }
}