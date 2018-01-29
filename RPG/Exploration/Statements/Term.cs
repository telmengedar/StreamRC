namespace StreamRC.RPG.Exploration.Statements {
    public class Term : ITerm {
        readonly string term;

        public Term(string term) {
            this.term = term;
        }

        public string GetTerm(Statement statement, int index) {
            return term;
        }
    }
}