namespace StreamRC.RPG.Exploration.Statements {
    public class PrepositionTerm : ITerm {

        public string GetTerm(Statement statement, int index) {
            if((statement.GetTerm(index + 1) as Noun)?.Countable ?? false)
                return "";

            switch(statement.GetTerm(index + 1).GetTerm(statement, index + 1)[0]) {
                case 'a':
                case 'A':
                case 'e':
                case 'E':
                case 'i':
                case 'I':
                case 'o':
                case 'O':
                case 'u':
                case 'U':
                case 'y':
                case 'Y':
                    return index == 0 ? "An" : "an";
                default:
                    return index == 0 ? "A" : "a";
            }
        }
    }
}