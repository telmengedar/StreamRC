using NightlyCode.Database.Entities;

namespace StreamRC.Core {

    /// <summary>
    /// interface for a database module
    /// </summary>
    public interface IDatabaseModule {

        /// <summary>
        /// database manager
        /// </summary>
        IEntityManager Database { get; }

        /// <summary>
        /// creates an new in-memory database
        /// </summary>
        /// <returns>entity manager to use to access database</returns>
        IEntityManager CreateMemoryDatabase();
    }
}