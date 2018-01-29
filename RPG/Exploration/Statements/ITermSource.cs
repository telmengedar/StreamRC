namespace StreamRC.RPG.Exploration.Statements {

    /// <summary>
    /// interface for a provider of a term
    /// </summary>
    public interface ITermSource {

        /// <summary>
        /// get term for a statement
        /// </summary>
        /// <returns>term</returns>
        ITerm GetTerm();
    }
}