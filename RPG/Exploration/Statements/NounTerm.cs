namespace StreamRC.RPG.Exploration.Statements {
    public class NounTerm : ITerm {
        readonly Noun noun;

        public NounTerm(Noun noun) {
            this.noun = noun;
        }

        public string GetTerm(Statement statement, int termindex) {
            return noun.Name;
        }
    }
}