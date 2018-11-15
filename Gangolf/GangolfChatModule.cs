using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Gangolf.Chat;
using NightlyCode.StreamRC.Gangolf.Dictionary;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Timer;
using StreamRC.Core.TTS;
using StreamRC.RPG.Messages;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace NightlyCode.StreamRC.Gangolf
{


    /// <summary>
    /// module providing gangolf interaction with chat
    /// </summary>
    [Dependency(nameof(RPGMessageModule))]
    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(TTSModule))]
    [Dependency(nameof(TimerModule))]
    [ModuleKey("gangolf")]
    public class GangolfChatModule : IRunnableModule, ITimerService {
        readonly Context context;
        const string voice = "CereVoice Stuart - English (Scotland)";
        ChatFactory factory;

        readonly object chatterlock = new object();
        readonly Dictionary<string, int> chatternames = new Dictionary<string, int>();

        readonly string[] reasontriggers = {
            "why", "reason"
        };

        readonly string[] explanationtriggers = {
            "wat", "wut", "what", "understand"
        };

        readonly string[] nametriggers = {
            "gangolf", "gasgolf", "gamgolf"
        };

        readonly string[] statictriggers = {
            "sorry", "sory"
        };

        readonly object greetlock = new object();
        readonly HashSet<Tuple<string, string>> greeted = new HashSet<Tuple<string, string>>();
        int chatterthingy = 0;

        /// <summary>
        /// creates a new <see cref="GangolfChatModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public GangolfChatModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            factory = context.GetModule<ChatFactory>();
            context.GetModule<StreamModule>().ChatMessage += OnChatMessage;
            context.GetModule<StreamModule>().UserLeft += OnUserLeft;
            context.GetModule<TimerModule>().AddService(this, 60.0);
        }

        void OnUserLeft(UserInformation user) {
            greeted.Remove(new Tuple<string, string>(user.Service, user.Username));
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().ChatMessage -= OnChatMessage;
        }

        void CreateInsult(long userid) {
            StringBuilder speech = new StringBuilder();
            if(RNG.XORShift64.NextFloat() < 0.15)
                speech.Append("Well, ");
            RPGMessageBuilder response = context.GetModule<RPGMessageModule>().Create();
            response.ShopKeeper();

            int mode = 0;

            switch(RNG.XORShift64.NextInt(21)) {
                case 0:
                    speech.Append("Let me point out that ");
                    response.Text(" points out that ");
                    break;
                case 1:
                    speech.Append("I think ");
                    response.Text(" thinks ");
                    break;
                case 2:
                    speech.Append("I imagine ");
                    response.Text(" imagines ");
                    break;
                case 3:
                    speech.Append("I declare: ");
                    response.Text(" declares ");
                    break;
                case 4:
                    speech.Append("I say ");
                    response.Text(" says ");
                    break;
                case 5:
                    speech.Append("I feel like ");
                    response.Text(" feels like ");
                    break;
                case 6:
                    speech.Append("Let me expose my ");
                    response.Text(" exposes his ");
                    mode = 1;
                    break;
                case 7:
                    speech.Append("I decided ");
                    response.Text(" decides ");
                    break;
                case 8:
                    speech.Append("I exclaim ");
                    response.Text(" exclaims ");
                    break;
                case 9:
                    speech.Append("I pontificate ");
                    response.Text(" pontificates ");
                    break;
                case 10:
                    speech.Append("I know ");
                    response.Text(" knows ");
                    break;
                case 11:
                    speech.Append("I realize ");
                    response.Text(" realizes ");
                    break;
                case 12:
                    speech.Append("I have come to understand ");
                    response.Text(" has come to understand ");
                    break;
                case 13:
                    speech.Append("I reached the conclusion ");
                    response.Text(" reached the conclusion ");
                    break;
                case 14:
                    speech.Append("Let's summarize ");
                    response.Text(" summarizes ");
                    break;
                case 15:
                    speech.Append("So it kind of sums up that ");
                    response.Text(" sums up that ");
                    break;
                case 16:
                    speech.Append("I distill ");
                    response.Text(" distills ");
                    break;
                case 17:
                    speech.Append("I've proven ");
                    response.Text(" has proven  ");
                    mode = 2;
                    break;
                case 18:
                    speech.Append("Let me demonstrate my ");
                    response.Text(" demonstrates his ");
                    mode = 1;
                    break;
                case 19:
                    speech.Append("I'm arranging ");
                    response.Text(" arranges ");
                    mode = 3;
                    break;
                case 20:
                    speech.Append("I research found ");
                    response.Text(" research finds ");
                    mode = 2;
                    break;
            }

            AppendInsult(response, speech, userid, mode);

            response.Send();
            context.GetModule<TTSModule>().Speak(speech.ToString(), voice);
        }

        void AppendInsult(RPGMessageBuilder response, StringBuilder speech, long userid, int mode=0) {
            if(mode != 1 && mode != 3) {
                speech.Append(context.GetModule<UserModule>().GetUser(userid).Name);
                response.User(userid);
                switch(RNG.XORShift64.NextInt(5)) {
                    case 0:
                        speech.Append("'s mother");
                        response.Text("'s mother");
                        break;
                    case 1:
                        speech.Append("'s father");
                        response.Text("'s father");
                        break;
                }
            }

            string insult;
            switch (mode)
            {
                default:
                case 0:
                    insult = factory.CreateInsult();
                    speech.Append($" is {(insult.StartsWithVocal() ? "an" : "a")} {insult}.");
                    response.Text($" is {(insult.StartsWithVocal() ? "an" : "a")} ").Text(insult);
                    break;
                case 1:
                    insult = factory.CreateInsult();
                    speech.Append($"{insult} to {context.GetModule<UserModule>().GetUser(userid).Name}.");
                    response.Text(insult).Text(" to ").User(userid);
                    break;
                case 2:
                    insult = factory.CreateInsult();
                    speech.Append($" to be {(insult.StartsWithVocal() ? "an" : "a")} {insult}.");
                    response.Text($" to be {(insult.StartsWithVocal() ? "an" : "a")} ").Text(insult);
                    break;
                case 3:
                    insult = factory.CreateInsult();
                    speech.Append($" {(insult.StartsWithVocal() ? "an" : "a")} {insult} for {context.GetModule<UserModule>().GetUser(userid).Name}.");
                    response.Text($" {(insult.StartsWithVocal() ? "an" : "a")} {insult} for ").User(userid);
                    break;
            }
        }

        long ScanForNames(string message) {
            string[] terms = message.Split(' ').Select(t=>t.Trim('?','!','.',',',';',':')).Where(t => nametriggers.All(n => n != t)).ToArray();
            return context.GetModule<UserModule>().FindUserIDs(terms).RandomItem(RNG.XORShift64);
        }

        void Greet(string service, string user) {
            StringBuilder speech = new StringBuilder();
            RPGMessageBuilder response = context.GetModule<RPGMessageModule>().Create();
            response.ShopKeeper();
            speech.Append("I ");

            string welcoming = $"{factory.Dictionary.GetWords(w => w.Class == WordClass.Verb && (w.Attributes & WordAttribute.Greeting) == WordAttribute.Greeting).RandomItem(RNG.XORShift64)}";
            speech.Append(welcoming.TrimEnd('s'));
            response.Text($" {welcoming} ");

            speech.Append(' ').Append(context.GetModule<UserModule>().GetUser(service, user).Name).Append(", the ");
            response.User(context.GetModule<UserModule>().GetUser(service, user)).Text(" the ");

            string insult = factory.CreateInsult();
            speech.Append(insult);
            response.Text(insult);

            context.GetModule<TTSModule>().Speak(speech.ToString(), voice);
            response.Send();
        }

        void OnChatMessage(IChatChannel channel, ChatMessage message) {
            lock(greetlock) {
                if(!greeted.Contains(new Tuple<string, string>(message.Service, message.User))) {
                    greeted.Add(new Tuple<string, string>(message.Service, message.User));
                    Greet(message.Service, message.User);
                    return;
                }
            }

            string lowermessage = message.Message.ToLower();

            if(nametriggers.Any(t => lowermessage.Contains(t))) {
                long name = ScanForNames(lowermessage);
                if(name == 0)
                    CreateInsult(context.GetModule<UserModule>().GetUserID(message.Service, message.User));
                else CreateInsult(name);
            }
            else if(reasontriggers.Any(t => lowermessage.Contains(t))) {
                CreateExplanation(context.GetModule<UserModule>().GetUserID(message.Service, message.User));
            }
            else if(explanationtriggers.Any(t => lowermessage.Contains(t)))
                Explain(context.GetModule<UserModule>().GetUserID(message.Service, message.User));
            else if(statictriggers.Any(t => lowermessage.Contains(t)))
                CreateInsult(context.GetModule<UserModule>().GetUserID(message.Service, message.User));
            else {
                if(factory.ContainsInsult(message.Message))
                    if(RNG.XORShift64.NextFloat() < 0.27f)
                        CreateInsult(context.GetModule<UserModule>().GetUserID(message.Service, message.User));
                    else {
                        lock(chatterlock) {
                            chatternames.TryGetValue(message.User, out int count);
                            chatternames[message.User] = count + 1;
                        }
                    }
            }
        }

        void CreateExplanation(long userid) {
            RPGMessageBuilder response = context.GetModule<RPGMessageModule>().Create();
            response.ShopKeeper();
            response.Text(" says it's quite simple. ");

            StringBuilder speech = new StringBuilder("It is quite simple. ");
            AppendInsult(response, speech, userid);
            response.Send();
            context.GetModule<TTSModule>().Speak(speech.ToString(), voice);
        }

        void Explain(long userid)
        {
            RPGMessageBuilder response = context.GetModule<RPGMessageModule>().Create();
            response.ShopKeeper();
            response.Text(" explains that ");

            StringBuilder speech = new StringBuilder("Let me explain that. ");
            AppendInsult(response, speech, userid);
            response.Send();
            context.GetModule<TTSModule>().Speak(speech.ToString(), voice);
        }

        void ITimerService.Process(double time) {
            lock(chatterlock) {
                if(chatternames.Count == 0)
                    return;
                
                KeyValuePair<string, int> victim = chatternames.RandomItem(kvp => kvp.Value, RNG.XORShift64);
                foreach(string key in chatternames.Keys.ToArray())
                    chatternames[key] = 0;

                if(victim.Value == 0 && ++chatterthingy < 10)
                    return;

                chatterthingy = 0;
                Exclaim(victim.Key);
            }
        }

        public void Exclaim(string name) {
            name = name.Split(' ')[0];

            StringBuilder sb = new StringBuilder();
            if(RNG.XORShift64.NextFloat() < 0.22) {
                Word exclamation = factory.Dictionary.GetRandomWord(w => w.Class == WordClass.Exclamation);
                if(exclamation != null)
                    sb.Append(exclamation.Text).Append("! ");
            }

            sb.Append(name).Append(" ");

            if(RNG.XORShift64.NextFloat() < 0.3) {
                Word amplifier = factory.Dictionary.GetRandomWord(w => w.Class == WordClass.Amplifier);
                if(amplifier != null)
                    sb.Append(amplifier.Text).Append(" ");
            }

            Word comparision = factory.Dictionary.GetRandomWord(w => (w.Attributes & WordAttribute.Comparision) != WordAttribute.None);
            sb.Append(comparision.Text).Append(" like ");
            string insultive = factory.InsultiveNoun();
            sb.Append(insultive.StartsWithVocal() ? "an " : "a ");
            sb.Append(insultive).Append("!");
            context.GetModule<TTSModule>().Speak(sb.ToString(), voice);
        }
    }
}
