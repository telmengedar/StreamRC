using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public bool ContainsInsult(string message) {
            return message.Split(' ').Select(w => w.ToLower()).Any(w => insults.Any(i => w.StartsWith(i) || w.EndsWith(i)));
        }

        public string CreateInsult() {
            HashSet<long> used=new HashSet<long>();

            StringBuilder text = new StringBuilder();

            Word adjective = dictionary.GetRandomWord(WordClass.Adjective, WordAttribute.None);
            WordAttribute attributefilter = WordAttribute.None;

            double chance = 0.36;
            do {
                

                if (adjective.Attributes.HasFlag(WordAttribute.Color))
                    attributefilter |= WordAttribute.Color;

                if (adjective.Attributes.HasFlag(WordAttribute.Political))
                    attributefilter |= WordAttribute.Political;

                text.Append(adjective).Append(' ');

                used.Add(adjective.ID);
                adjective = dictionary.GetRandomWord(WordClass.Adjective, attributefilter, used.ToArray());
                chance *= chance;
            }
            while(RNG.XORShift64.NextDouble() < chance);
            text.Append(adjective).Append(' ');
            used.Clear();

            Word noun;
            if (RNG.XORShift64.NextDouble() < 0.3) {
                Word verb = dictionary.GetRandomWord(WordClass.AdjectiveCont, WordAttribute.None);

                if(!verb.Attributes.HasFlag(WordAttribute.Insultive)) {
                    noun = dictionary.GetRandomWord(WordClass.Noun, WordAttribute.Insultive);
                    text.Append(noun).Append('-');
                    used.Add(noun.ID);
                }

                text.Append(verb).Append(' ');
            }

            noun = dictionary.GetRandomWord(WordClass.Noun, WordAttribute.Object, used.ToArray());

            used.Add(noun.ID);

            WordAttribute predicate = WordAttribute.Descriptive;
            if(noun.Attributes.HasFlag(WordAttribute.Product))
                predicate |= WordAttribute.Producer;
            if(!noun.Attributes.HasFlag(WordAttribute.Insultive))
                predicate |= WordAttribute.Insultive;

            Word descriptive = dictionary.GetRandomWord(WordClass.Noun, predicate, used.ToArray());
            text.Append($"{descriptive.Text}-{noun.Text}");
            if(noun.Class==WordClass.Noun && noun.Group > 0 && RNG.XORShift64.NextFloat() < 0.07) {
                Word postposition = dictionary.GetRandomLinkedWord(WordClass.Postposition, WordAttribute.None, noun.Group, used.ToArray());
                if(postposition != null)
                    text.Append(postposition);
            }
            return text.ToString();
        }

        public string InsultiveNoun() {
            Word noun = dictionary.GetRandomWord(WordClass.Noun, WordAttribute.Object);

            WordAttribute predicate = WordAttribute.Descriptive;
            if (noun.Attributes.HasFlag(WordAttribute.Product))
                predicate |= WordAttribute.Producer;
            if (!noun.Attributes.HasFlag(WordAttribute.Insultive))
                predicate |= WordAttribute.Insultive;

            Word descriptive = dictionary.GetRandomWord(WordClass.Noun, predicate, noun.ID);
            if (noun.Class == WordClass.Noun && noun.Group > 0 && RNG.XORShift64.NextFloat() < 0.07) {
                Word postposition = dictionary.GetRandomWord(WordClass.Postposition, WordAttribute.None, noun.Group);
                if (postposition != null)
                    return $"{descriptive.Text}-{noun.Text}{postposition}";
            }

            return $"{descriptive.Text}-{noun.Text}";
        }

        public string DescriptiveInsultiveNoun() {
            StringBuilder text = new StringBuilder();

            Word adjective = dictionary.GetRandomWord(WordClass.Adjective, WordAttribute.None);

            text.Append(adjective.Text).Append(" ");

            Word noun = dictionary.GetRandomWord(WordClass.Noun, WordAttribute.Object);

            WordAttribute predicate = WordAttribute.Descriptive;
            if (noun.Attributes.HasFlag(WordAttribute.Product))
                predicate |= WordAttribute.Producer;
            if (!noun.Attributes.HasFlag(WordAttribute.Insultive))
                predicate |= WordAttribute.Insultive;

            Word descriptive = dictionary.GetRandomWord(WordClass.Noun, predicate, adjective.ID, noun.ID);
            text.Append($"{descriptive.Text}-{noun.Text}");
            if (noun.Class == WordClass.Noun && noun.Group > 0 && RNG.XORShift64.NextFloat() < 0.07) {
                Word postposition = dictionary.GetRandomWord(WordClass.Postposition, WordAttribute.None, noun.Group);
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