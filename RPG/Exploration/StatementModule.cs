using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Data;
using NightlyCode.DB.Entities;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Data;
using StreamRC.RPG.Exploration.Data;
using StreamRC.RPG.Exploration.Statements;

namespace StreamRC.RPG.Exploration {

    /// <summary>
    /// module used to create text statments
    /// </summary>
    [Dependency(nameof(RPGDatabaseModule), DependencyType.Type)]
    public class StatementModule : IInitializableModule {
        readonly Context context;
        IEntityManager rpgdatabase;

        readonly Dictionary<string, ITermSource> sources = new Dictionary<string, ITermSource>();
         
        /// <summary>
        /// creates a new <see cref="StatementModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public StatementModule(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            rpgdatabase = context.GetModule<RPGDatabaseModule>().Database;
            rpgdatabase.UpdateSchema<Noun>();
            rpgdatabase.UpdateSchema<Adjective>();

            sources["Noun"] = new NounSource(rpgdatabase);
            sources["Adjective"] = new AdjectiveSource(rpgdatabase);
            sources["Preposition"] = new PrepositionSource();

            foreach (Noun noun in DataTable.ReadCSV(ResourceAccessor.GetResource<Stream>("StreamRC.RPG.Resources.nouns.csv"), '\t', true).Deserialize<Noun>())
                rpgdatabase.Insert<Noun>().Columns(n => n.Name, n => n.Countable).Values(noun.Name, noun.Countable).Execute();
            foreach (Adjective adjective in DataTable.ReadCSV(ResourceAccessor.GetResource<Stream>("StreamRC.RPG.Resources.adjectives.csv"), '\t', true).Deserialize<Adjective>())
                rpgdatabase.Insert<Adjective>().Columns(a=>a.Name).Values(adjective.Name).Execute();
        }

        string ExtractField(string text, ref int index, char terminator) {
            int startindex = index;
            while(index < text.Length && text[index] != terminator)
                ++index;
            return text.Substring(startindex, index - startindex);
        }

        IEnumerable<ITermSource> ParseSources(string statement, string[] arguments) {
            int startindex = 0;
            for(int i = 0; i < statement.Length; ++i) {
                switch(statement[i]) {
                    case '[':
                        if(i > startindex)
                            yield return new ConstantTermSource(statement.Substring(startindex, i - startindex));
                        ++i;
                        yield return sources[ExtractField(statement, ref i, ']')];
                        startindex = i + 1;
                        break;
                    case '$':
                        if (i > startindex)
                            yield return new ConstantTermSource(statement.Substring(startindex, i - startindex));
                        ++i;
                        yield return new ConstantTermSource(arguments[int.Parse(ExtractField(statement, ref i, ' '))]);
                        startindex = i;
                        break;
                }
            }

            if(startindex < statement.Length)
                yield return new ConstantTermSource(statement.Substring(startindex, statement.Length - startindex));
        }

        public Statement CreateStatement(string statement, params string[] arguments) {
            return new Statement(ParseSources(statement, arguments).ToArray());
        }
    }
}