using System.Collections.Generic;
using System.Linq;

namespace StreamRC.RPG.Exploration.Statements {
    public class Statement {
        readonly ITermSource[] sources;
        readonly ITerm[] terms;

        public Statement(ITermSource[] sources) {
            this.sources = sources;
            terms = new ITerm[sources.Length];
        }

        public int Count => sources.Length;

        public ITerm GetTerm(int index) {
            if(terms[index] == null)
                terms[index] = sources[index].GetTerm();
            return terms[index];
        }

        IEnumerable<string> Terms
        {
            get
            {
                for(int i = 0; i < Count; ++i)
                    yield return GetTerm(i).GetTerm(this, i);
            }
        }
             
        public string GetStatement() {
            return string.Join("", Terms.Where(t => !string.IsNullOrEmpty(t)));
        }
    }
}