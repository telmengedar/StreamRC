namespace StreamRC.RPG.Exploration.Statements {

    /// <summary>
    /// interface for a term in a statement
    /// </summary>
    public interface ITerm {
        /// <summary>
        /// get term to use for the statement
        /// </summary>
        /// <param name="statement">statement containing the term</param>
        /// <param name="termindex">index of this term in statement</param>
        /// <returns>string which represents the term</returns>
        string GetTerm(Statement statement, int termindex);
    }
}