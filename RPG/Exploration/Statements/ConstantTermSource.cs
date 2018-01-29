namespace StreamRC.RPG.Exploration.Statements {
    public class ConstantTermSource : ITermSource {
        readonly string term;

        public ConstantTermSource(string term) {
            this.term = term;
        }

        public ITerm GetTerm() {
            return new Term(term);
        }
    }
}