namespace StreamRC.RPG.Exploration.Statements {
    public class PrepositionSource : ITermSource {
        public ITerm GetTerm() {
            return new PrepositionTerm();
        }
    }
}