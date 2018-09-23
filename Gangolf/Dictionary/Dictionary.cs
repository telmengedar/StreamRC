using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Data;
using NightlyCode.DB.Clients;
using NightlyCode.DB.Entities;
using NightlyCode.DB.Entities.Operations;

namespace NightlyCode.StreamRC.Gangolf.Dictionary {

    /// <summary>
    /// dictionary containing words and information about them
    /// </summary>
    public class Dictionary {
        readonly EntityManager dictionary = new EntityManager(DBClient.CreateSQLite(null, false));

        public Dictionary() {
            dictionary.Create<Word>();

            DataTable data = DataTable.ReadCSV(ResourceAccessor.GetResource<Stream>("NightlyCode.StreamRC.Gangolf.Dictionary.dictionary.csv"), ';', true);
            for(int row = 0; row < data.RowCount; ++row) {
                WordAttribute attributes = WordAttribute.None;
                if(!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Insultive")))
                    attributes |= WordAttribute.Insultive;
                if(!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Romantic")))
                    attributes |= WordAttribute.Romantic;
                if(!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Product")))
                    attributes |= WordAttribute.Product;
                if(!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Tool")))
                    attributes |= WordAttribute.Tool;
                if(!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Producer")))
                    attributes |= WordAttribute.Producer;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Color")))
                    attributes |= WordAttribute.Color;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Political")))
                    attributes |= WordAttribute.Political;
                if (!string.IsNullOrEmpty(data.TryGetValue<string>(row, "Descriptive")))
                    attributes |= WordAttribute.Descriptive;

                int group;
                int.TryParse(data.TryGetValue<string>(row, "Conjunktion"), out group);

                foreach(Tuple<string, WordClass> word in ExtractWord(data, row)) {
                    WordAttribute termattributes = attributes;
                    if(word.Item2 == WordClass.Subject)
                        termattributes |= WordAttribute.Object;

                    dictionary.Insert<Word>().Columns(w => w.Text, w => w.Class, w => w.Attributes, w => w.Group)
                        .Values(word.Item1, word.Item2, termattributes, group)
                        .Execute();
                }
            }
        }

        IEnumerable<Tuple<string, WordClass>> ExtractWord(DataTable table, int row) {
            if(!string.IsNullOrEmpty(table[row, 0]))
                yield return new Tuple<string, WordClass>(table[row, 0], WordClass.Noun);
            if (!string.IsNullOrEmpty(table[row, 1]))
                yield return new Tuple<string, WordClass>(table[row, 1], WordClass.Verb);
            if (!string.IsNullOrEmpty(table[row, 2]))
                yield return new Tuple<string, WordClass>(table[row, 2], WordClass.Adjective);
            if (!string.IsNullOrEmpty(table[row, 3]))
                yield return new Tuple<string, WordClass>(table[row, 3], WordClass.Subject);
            if (!string.IsNullOrEmpty(table[row, 4]))
                yield return new Tuple<string, WordClass>(table[row, 4], WordClass.Postposition);
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
        /// get all words from the dictionary which match the specified <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">predicate the words have to match</param>
        /// <returns>all words matching the specified <paramref name="predicate"/></returns>
        public IEnumerable<Word> GetWords(Expression<Func<Word, bool>> predicate) {
            return dictionary.LoadEntities<Word>().Where(predicate).OrderBy(new OrderByCriteria(DBFunction.Random)).Execute();
        }
    }
}