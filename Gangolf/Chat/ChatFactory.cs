using System.Collections.Generic;
using System.Linq;
using System.Text;
using NightlyCode.Core.Randoms;
using NightlyCode.StreamRC.Gangolf.Dictionary;

namespace NightlyCode.StreamRC.Gangolf.Chat {
    public class ChatFactory {
        readonly HashSet<string> insults = new HashSet<string>();
        readonly Dictionary.Dictionary dictionary = new Dictionary.Dictionary();

        public ChatFactory() {
            foreach(Word insult in dictionary.GetWords(w => (w.Attributes & WordAttribute.Insultive) == WordAttribute.Insultive))
                insults.Add(insult.Text.ToLower());
        }

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
                chance *= 0.25;
            }
            while(RNG.XORShift64.NextDouble() < chance);
            text.Append(adjective).Append(' ');
            used.Clear();

            Word noun;
            if (RNG.XORShift64.NextDouble() < 0.3) {
                Word verb = dictionary.GetRandomWord(w => (w.Class & WordClass.Verb) == WordClass.Verb);

                if(!verb.Attributes.HasFlag(WordAttribute.Insultive)) {
                    noun = dictionary.GetRandomWord(w => w.Class == WordClass.Noun && (w.Attributes & WordAttribute.Insultive) == WordAttribute.Insultive);
                    text.Append(noun).Append('-');
                    used.Add(noun.ID);
                }

                text.Append(verb).Append(' ');
            }

            if(used.Count > 0) {
                usedids = used.ToArray();
                noun = dictionary.GetRandomWord(w => (w.Class & (WordClass.Noun | WordClass.Subject)) != WordClass.None && w.ID != usedids[0]);
            }
            else noun = dictionary.GetRandomWord(w => (w.Class & (WordClass.Noun | WordClass.Subject)) != WordClass.None);

            used.Add(noun.ID);

            WordAttribute predicate = WordAttribute.Descriptive;
            if(noun.Attributes.HasFlag(WordAttribute.Product))
                predicate |= WordAttribute.Producer;
            if(!noun.Attributes.HasFlag(WordAttribute.Insultive))
                predicate |= WordAttribute.Insultive;

            usedids = used.ToArray();
            Word descriptive = dictionary.GetRandomWord(w => (w.Class & WordClass.Noun) != WordClass.None && (w.Attributes & predicate) == predicate && !usedids.Contains(w.ID));
            text.Append($"{descriptive.Text}{noun.Text}");
            return text.ToString();
        }
    }
}