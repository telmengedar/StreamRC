using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using NightlyCode.Core.Data;
using NightlyCode.Core.Logs;
using NightlyCode.Database.Clients;
using NightlyCode.Database.Entities;
using NightlyCode.Database.Entities.Operations;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Database.Entities.Operations.Prepared;
using NightlyCode.Database.Info;
using NightlyCode.Modules;

namespace NightlyCode.StreamRC.Gangolf.Dictionary {

    /// <summary>
    /// dictionary containing words and information about them
    /// </summary>
    [Module(Key="dictionary")]
    public class DictionaryModule {
        readonly IEntityManager dictionary = new EntityManager(new DBClient(new SqliteConnection("Data Source=dictionary.db3"), new SQLiteInfo()));

        readonly PreparedLoadEntitiesOperation<Word> loadrandomwordattributesmatch;
        readonly PreparedLoadEntitiesOperation<Word> loadrandomwordattributeflag;
        readonly PreparedLoadEntitiesOperation<Word> loadrandomwordattributesmatchexcept;
        readonly PreparedLoadEntitiesOperation<Word> loadrandomwordattributeflagexcept;
        readonly PreparedLoadEntitiesOperation<Word> loadrandomlinkedword;
        readonly PreparedLoadEntitiesOperation<Word> loadrandomlinkedwordexcept;

        /// <summary>
        /// creates a new <see cref="DictionaryModule"/>
        /// </summary>
        public DictionaryModule() {
            dictionary.UpdateSchema<Word>();
            loadrandomwordattributesmatch = dictionary.LoadEntities<Word>()
                .Where(w => w.Class == DBParameter<WordClass>.Value 
                            && (w.Attributes & DBParameter<WordAttribute>.Index(2).Data) == DBParameter<WordAttribute>.Index(2).Data)
                .OrderBy(new OrderByCriteria(DBFunction.Random))
                .Limit(1)
                .Prepare();

            loadrandomwordattributeflag = dictionary.LoadEntities<Word>()
                .Where(w => w.Class == DBParameter<WordClass>.Value
                            && (w.Attributes & DBParameter<WordAttribute>.Value) != WordAttribute.None)
                .OrderBy(new OrderByCriteria(DBFunction.Random))
                .Limit(1)
                .Prepare();

            loadrandomwordattributesmatchexcept = dictionary.LoadEntities<Word>()
                .Where(w => w.Class == DBParameter<WordClass>.Value
                            && (w.Attributes & DBParameter<WordAttribute>.Index(2).Data) == DBParameter<WordAttribute>.Index(2).Data
                            && !DBParameter<long[]>.Value.Contains(w.ID))
                .OrderBy(new OrderByCriteria(DBFunction.Random))
                .Limit(1)
                .Prepare();

            loadrandomwordattributeflagexcept = dictionary.LoadEntities<Word>()
                .Where(w => w.Class == DBParameter<WordClass>.Value
                            && (w.Attributes & DBParameter<WordAttribute>.Value) != WordAttribute.None
                            && !DBParameter<long[]>.Value.Contains(w.ID))
                .OrderBy(new OrderByCriteria(DBFunction.Random))
                .Limit(1)
                .Prepare();

            loadrandomlinkedword = dictionary.LoadEntities<Word>()
                .Where(w => w.Class == DBParameter<WordClass>.Value
                            && (w.Attributes & DBParameter<WordAttribute>.Index(2).Data) == DBParameter<WordAttribute>.Index(2).Data
                            && (w.Group & DBParameter.Int32) != 0)
                .OrderBy(new OrderByCriteria(DBFunction.Random))
                .Limit(1)
                .Prepare();

            loadrandomlinkedwordexcept = dictionary.LoadEntities<Word>()
                .Where(w => w.Class == DBParameter<WordClass>.Value
                            && (w.Attributes & DBParameter<WordAttribute>.Index(2).Data) == DBParameter<WordAttribute>.Index(2).Data
                            && (w.Group & DBParameter.Int32) != 0
                            && !DBParameter<long[]>.Value.Contains(w.ID))
                .OrderBy(new OrderByCriteria(DBFunction.Random))
                .Limit(1)
                .Prepare();
        }

        /// <summary>
        /// triggered when a word was added (or updated)
        /// </summary>
        public event Action<Word> WordAdded;

        /// <summary>
        /// adds a word to the dictionary
        /// </summary>
        /// <param name="text">text of word to add</param>
        /// <param name="class">word class to add</param>
        /// <param name="attributes">attributes of word</param>
        public void Add(string text, WordClass @class, WordAttribute attributes) {
            if(dictionary.Update<Word>().Set(w => w.Attributes == (w.Attributes|attributes)).Where(w => w.Class == @class && w.Text == text).Execute() == 0)
                dictionary.Insert<Word>().Columns(w => w.Text, w => w.Class, w => w.Attributes, w=>w.Group)
                    .Values(text, @class, attributes, 0)
                    .Execute();

            WordAdded?.Invoke(new Word {
                Text = text,
                Class = @class,
                Attributes = attributes
            });
        }

        /// <summary>
        /// removes attributes of a word
        /// </summary>
        /// <param name="text">word text</param>
        /// <param name="class">word class</param>
        /// <param name="attributes">attributes to remove</param>
        public void RemoveAttribute(string text, WordClass @class, WordAttribute attributes) {
            attributes = ~attributes;
            dictionary.Update<Word>().Set(w => w.Attributes == (w.Attributes & attributes)).Where(w => w.Class == @class && w.Text == text).Execute();
        }

        /// <summary>
        /// removes a word from dictionary
        /// </summary>
        /// <param name="text">word characters</param>
        /// <param name="class">word class</param>
        public void Remove(string text, WordClass @class) {
            dictionary.Delete<Word>().Where(w => w.Text == text && w.Class == @class).Execute();
        }

        /// <summary>
        /// loads or reloads the contents of the dictionary
        /// </summary>
        /// <param name="stream"></param>
        public void Load(Stream stream) {
            Logger.Info(this, "Loading dictionary");
            dictionary.Delete<Word>().Execute();

            DataTable data = DataTable.ReadCSV(stream, ';', true);
            for (int row = 0; row < data.RowCount; ++row)
            {
                WordAttribute attributes = WordAttribute.None;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Insultive")))
                    attributes |= WordAttribute.Insultive;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Romantic")))
                    attributes |= WordAttribute.Romantic;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Product")))
                    attributes |= WordAttribute.Product;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Tool")))
                    attributes |= WordAttribute.Tool;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Producer")))
                    attributes |= WordAttribute.Producer;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Color")))
                    attributes |= WordAttribute.Color;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Political")))
                    attributes |= WordAttribute.Political;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Descriptive")))
                    attributes |= WordAttribute.Descriptive;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Greeting")))
                    attributes |= WordAttribute.Greeting;

                int.TryParse(data.TryGetValue<string>(row, "Conjunktion"), out int group);

                foreach (Tuple<string, WordClass> word in ExtractWord(data, row))
                {
                    WordAttribute termattributes = attributes;
                    if (word.Item2 == WordClass.Subject)
                        termattributes |= WordAttribute.Object;

                    dictionary.Insert<Word>().Columns(w => w.Text, w => w.Class, w => w.Attributes, w => w.Group)
                        .Values(word.Item1, word.Item2, termattributes, group)
                        .Execute();
                }
            }

            Logger.Info(this, "Dictionary loaded");
        }

        IEnumerable<Tuple<string, WordClass>> ExtractWord(DataTable table, int row) {
            if(!string.IsNullOrEmpty(table[row, 0]))
                yield return new Tuple<string, WordClass>(table[row, 0], WordClass.Noun);
            if (!string.IsNullOrEmpty(table[row, 1]))
                yield return new Tuple<string, WordClass>(table[row, 1], WordClass.Verb);
            if (!string.IsNullOrEmpty(table[row, 2]))
                yield return new Tuple<string, WordClass>(table[row, 2], WordClass.AdjectiveCont);
            if (!string.IsNullOrEmpty(table[row, 3]))
                yield return new Tuple<string, WordClass>(table[row, 3], WordClass.Adjective);
            if (!string.IsNullOrEmpty(table[row, 4]))
                yield return new Tuple<string, WordClass>(table[row, 4], WordClass.Subject);
            if (!string.IsNullOrEmpty(table[row, 5]))
                yield return new Tuple<string, WordClass>(table[row, 5], WordClass.Postposition);
        }

        /// <summary>
        /// gets a random word from the dictionary which matches the <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate"><paramref name="predicate"/> for the word to match</param>
        /// <returns>random word which matches the <paramref name="predicate"/></returns>
        public Word GetRandomWord(Expression<Func<Word, bool>> predicate) {
            return dictionary.LoadEntities<Word>().Where(predicate).OrderBy(new OrderByCriteria(DBFunction.Random)).Limit(1).Execute().FirstOrDefault();
        }

        /// <summary>
        /// gets a random word which matches the specified class and attributes
        /// </summary>
        /// <param name="class">class word has to match</param>
        /// <param name="attributes">attributes word has to implement</param>
        /// <param name="matchallattributes">whether to match all or just one attribute</param>
        /// <param name="except">ids of words not to include in result</param>
        /// <returns></returns>
        public Word GetRandomWord(WordClass @class, WordAttribute attributes, bool matchallattributes=true, params long[] except) {
            Word word;
            if(matchallattributes) {
                if(except.Length == 0)
                    word = loadrandomwordattributesmatch.Execute(@class, attributes).FirstOrDefault();
                else word = loadrandomwordattributesmatchexcept.Execute(@class, attributes, except).FirstOrDefault();
            }
            else {
                if (except.Length == 0)
                    word = loadrandomwordattributeflag.Execute(@class, attributes).FirstOrDefault();
                else word = loadrandomwordattributeflagexcept.Execute(@class, attributes, except).FirstOrDefault();
            }

            if (word == null)
                throw new Exception($"No word in database which matches ({@class}, {attributes}) except ({string.Join(",", except)})");
            return word;
        }

        /// <summary>
        /// gets a random word which matches the specified class and attributes
        /// </summary>
        /// <param name="class">class word has to match</param>
        /// <param name="attributes">attributes word has to implement</param>
        /// <param name="group">word group word is linked to</param>
        /// <param name="except">ids of words not to include in result</param>
        /// <returns></returns>
        public Word GetRandomLinkedWord(WordClass @class, WordAttribute attributes, int group, params long[] except)
        {
            if (except.Length == 0)
                return loadrandomlinkedword.Execute(@class, attributes).FirstOrDefault();
            return loadrandomlinkedwordexcept.Execute(@class, attributes, except).FirstOrDefault();
        }

        /// <summary>
        /// get all words from the dictionary which match the specified <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">predicate the words have to match</param>
        /// <returns>all words matching the specified <paramref name="predicate"/></returns>
        public IEnumerable<Word> ListWords(Expression<Func<Word, bool>> predicate) {
            return dictionary.LoadEntities<Word>().Where(predicate).OrderBy(new OrderByCriteria(DBFunction.Random)).Execute();
        }

        /// <summary>
        /// lists words of the specified class implementing the specified attributes
        /// </summary>
        /// <param name="class">class of words to list</param>
        /// <param name="attributes">attributes of words to implement</param>
        /// <returns></returns>
        public IEnumerable<Word> ListWords(WordClass @class, WordAttribute attributes) {
            return ListWords(w => w.Class == @class && w.Attributes == attributes);
        }
    }
}