using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;

namespace StreamRC.RPG.Shops {

    /// <summary>
    /// evaluates messages for several attributes
    /// </summary>
    public class MessageEvaluator {

        readonly string[] insultkeywords = {
            "ass", "fuck", "bitch", "cunt", "whore", "penis", "arse", "butt", "retard", "bum", "shit", "idiot", "dildo", "dick", "cock", "suck"
        };

        readonly string[] adjective = {
            "fucked", "fucking", "bitchy", "whoring", "retarded", "shitting", "sucking"
        };

        readonly string[] preobject = {
            "ass", "fuck", "arse", "butt", "shit"
        };

        readonly string[] defobject = {
            "bitch", "cunt", "whore", "penis", "dildo", "dick", "cock", "bum"
        };

        public string CreateInsult() {
            return $"{adjective.RandomItem(RNG.XORShift64)} {preobject.RandomItem(RNG.XORShift64)}{defobject.RandomItem(RNG.XORShift64)}";
        }

        /// <summary>
        /// determines whether the message contains an insult
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HasInsult(string message) {
            string lowercase = message.ToLower();
            return insultkeywords.Any(w => lowercase.Contains(w));
        }

        /// <summary>
        /// determines whether arguments contain an insult
        /// </summary>
        /// <param name="arguments">arguments to scan</param>
        /// <param name="argumentindex">index at which to begin scanning</param>
        /// <returns>true if arguments contain an insult, false otherwise</returns>
        public bool HasInsult(string[] arguments, int argumentindex=0)
        {
            for (int i = argumentindex; i < arguments.Length; ++i)
            {
                string caseless = arguments[i].ToLower();
                if (insultkeywords.Any(w => caseless.Contains(w)))
                    return true;
            }
            return false;
        }

    }
}