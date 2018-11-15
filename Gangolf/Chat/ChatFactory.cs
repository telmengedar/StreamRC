using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NightlyCode.Core.Helpers;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Randoms;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Gangolf.Dictionary;

namespace NightlyCode.StreamRC.Gangolf.Chat {

    [Module]
    public class ChatFactory {
        readonly HashSet<string> insults = new HashSet<string>();
        readonly DictionaryModule dictionary;

        public ChatFactory(DictionaryModule dictionary) {
            this.dictionary = dictionary;

            dictionary.WordAdded += OnWordAdded;
            ReloadInsults();
        }

        public void LoadDictionary() {
            string file = Path.Combine(PathExtensions.GetApplicationDirectory(), "data", "dictionary.csv");
            if(!File.Exists(file))
                return;

            using(Stream dictionarydata = File.OpenRead(file))
                dictionary.Load(dictionarydata);
        }

        public DictionaryModule Dictionary => dictionary;

        public bool ContainsInsult(string message) {
            return message.Split(' ').Select(w => w.ToLower()).Any(w => insults.Any(i => w.StartsWith(i) || w.EndsWith(i)));
        }

        public string CreateInsult() {
            HashSet<long> used=new HashSet<long>();
            long[] usedids;

            StringBuilder text = new StringBuilder();

            Word adjective = dictionary.GetRandomWord(w => (w.Class & WordClass.Adjective) == WordClass.Adjective);
            WordAttribute attributefilter = WordAttribute.None;

            double chance = 0.36;
            do {
                

                if (adjective.Attributes.HasFlag(WordAttribute.Color))
                    attributefilter |= WordAttribute.Color;

                if (adjective.Attributes.HasFlag(WordAttribute.Political))
                    attributefilter |= WordAttribute.Political;

                text.Append(adjective).Append(' ');

                used.Add(adjective.ID);
                usedids = used.ToArray();
                adjective = dictionary.GetRandomWord(w => (w.Class & WordClass.Adjective) == WordClass.Adjective
                                                          && (w.Attributes & attributefilter) == WordAttribute.None
                                                          && !usedids.Contains(w.ID));
                chance *= chance;
            }
            while(RNG.XORShift64.NextDouble() < chance);
            text.Append(adjective).Append(' ');
            used.Clear();

            Word noun;
            if (RNG.XORShift64.NextDouble() < 0.3) {
                Word verb = dictionary.GetRandomWord(w => (w.Class & WordClass.AdjectiveCont) == WordClass.AdjectiveCont);

                if(!verb.Attributes.HasFlag(WordAttribute.Insultive)) {
                    noun = dictionary.GetRandomWord(w => w.Class == WordClass.Noun && (w.Attributes & WordAttribute.Insultive) == WordAttribute.Insultive);
                    text.Append(noun).Append('-');
                    used.Add(noun.ID);
                }

                text.Append(verb).Append(' ');
            }

            if(used.Count > 0) {
                usedids = used.ToArray();
                noun = dictionary.GetRandomWord(w => (w.Class & (WordClass.Noun | WordClass.Subject)) != WordClass.None && (w.Attributes & WordAttribute.Object) != WordAttribute.None && w.ID != usedids[0]);
            }
            else noun = dictionary.GetRandomWord(w => (w.Class & (WordClass.Noun | WordClass.Subject)) != WordClass.None && (w.Attributes & WordAttribute.Object) != WordAttribute.None);

            used.Add(noun.ID);

            WordAttribute predicate = WordAttribute.Descriptive;
            if(noun.Attributes.HasFlag(WordAttribute.Product))
                predicate |= WordAttribute.Producer;
            if(!noun.Attributes.HasFlag(WordAttribute.Insultive))
                predicate |= WordAttribute.Insultive;

            usedids = used.ToArray();
            Word descriptive = dictionary.GetRandomWord(w => (w.Class & WordClass.Noun) != WordClass.None && (w.Attributes & predicate) == predicate && !usedids.Contains(w.ID));
            text.Append($"{descriptive.Text}-{noun.Text}");
            if(noun.Class==WordClass.Noun && noun.Group > 0 && RNG.XORShift64.NextFloat() < 0.07) {
                Word postposition = dictionary.GetRandomWord(w => (w.Class & WordClass.Postposition) == WordClass.Postposition && (w.Group & noun.Group) != 0);
                if(postposition != null)
                    text.Append(postposition);
            }
            return text.ToString();
        }

        public string InsultiveNoun() {
            StringBuilder text=new StringBuilder();

            Word adjective = dictionary.GetRandomWord(w => (w.Class & WordClass.Adjective) == WordClass.Adjective);

            text.Append(adjective.Text).Append(" ");
            List<long> usedids = new List<long>() {
                adjective.ID
            };

            Word noun = dictionary.GetRandomWord(w => (w.Class & (WordClass.Noun | WordClass.Subject)) != WordClass.None && (w.Attributes & WordAttribute.Object) != WordAttribute.None);

            usedids.Add(noun.ID);

            WordAttribute predicate = WordAttribute.Descriptive;
            if (noun.Attributes.HasFlag(WordAttribute.Product))
                predicate |= WordAttribute.Producer;
            if (!noun.Attributes.HasFlag(WordAttribute.Insultive))
                predicate |= WordAttribute.Insultive;

            long[] used = usedids.ToArray();
            Word descriptive = dictionary.GetRandomWord(w => (w.Class & WordClass.Noun) != WordClass.None && (w.Attributes & predicate) == predicate && !used.Contains(w.ID));
            text.Append($"{descriptive.Text}-{noun.Text}");
            if (noun.Class == WordClass.Noun && noun.Group > 0 && RNG.XORShift64.NextFloat() < 0.07)
            {
                Word postposition = dictionary.GetRandomWord(w => (w.Class & WordClass.Postposition) == WordClass.Postposition && (w.Group & noun.Group) != 0);
                if (postposition != null)
                    text.Append(postposition);
            }

            return text.ToString();
        }

        void ReloadInsults() {
            Logger.Info(this, "Reloading insults");
            insults.Clear();
            foreach (Word insult in dictionary.GetWords(w => (w.Attributes & WordAttribute.Insultive) == WordAttribute.Insultive))
                insults.Add(insult.Text.ToLower());
        }

        void OnWordAdded(Word word) {
            if((word.Attributes & WordAttribute.Insultive) != WordAttribute.None)
                ReloadInsults();
        }
    }
}