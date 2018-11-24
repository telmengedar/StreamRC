using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Gangolf.Chat;
using NightlyCode.StreamRC.Gangolf.Dictionary;
using StreamRC.Core.Messages;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Chat;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace NightlyCode.StreamRC.Gangolf
{


    /// <summary>
    /// module providing gangolf interaction with chat
    /// </summary>
    [Module(Key="gangolf", AutoCreate = true)]
    public class GangolfChatModule : ITimerService {
        const string voice = "CereVoice Stuart - English (Scotland)";
        const string gangolfpic = "NightlyCode.StreamRC.Gangolf.Resources.shopkeeper_normal.png";

        readonly ChatFactory factory;
        readonly ChatMessageModule messages;
        readonly UserModule usermodule;
        readonly ImageCacheModule images;
        readonly DictionaryModule dictionary;

        readonly object chatterlock = new object();
        readonly Dictionary<string, int> chatternames = new Dictionary<string, int>();

        readonly string[] explanationtriggers = {
            "wat", "wut", "what", "understand", "why", "reason"
        };

        readonly string[] nametriggers = {
            "gangolf", "gasgolf", "gamgolf"
        };

        readonly string[] statictriggers = {
            "sorry", "sory"
        };

        readonly object greetlock = new object();
        readonly HashSet<Tuple<string, string>> greeted = new HashSet<Tuple<string, string>>();
        int chatterthingy;

        /// <summary>
        /// creates a new <see cref="GangolfChatModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public GangolfChatModule(ChatFactory factory, StreamModule stream, TimerModule timer, ChatMessageModule messages, UserModule usermodule, ImageCacheModule images, DictionaryModule dictionary) {
            this.factory = factory;
            this.messages = messages;
            this.usermodule = usermodule;
            this.images = images;
            this.dictionary = dictionary;
            stream.ChatMessage += OnChatMessage;
            stream.UserLeft += OnUserLeft;
            timer.AddService(this, 60.0);
        }

        void OnUserLeft(UserInformation user) {
            greeted.Remove(new Tuple<string, string>(user.Service, user.Username));
        }

        void CreateInsult(long userid) {
            MessageBuilder response = new MessageBuilder();

            response.Image(images.GetImageByResource(GetType().Assembly, gangolfpic));
            if (RNG.XORShift64.NextFloat() < 0.15)
                response.Text("Well, ");

            int mode = 0;

            switch(RNG.XORShift64.NextInt(21)) {
                case 0:
                    response.Text("Let me point out that ");
                    break;
                case 1:
                    response.Text("I think ");
                    break;
                case 2:
                    response.Text("I imagine ");
                    break;
                case 3:
                    response.Text("I declare: ");
                    break;
                case 4:
                    response.Text("I say ");
                    break;
                case 5:
                    response.Text("I feel like ");
                    break;
                case 6:
                    response.Text("Let me expose my ");
                    mode = 1;
                    break;
                case 7:
                    response.Text("I decided ");
                    break;
                case 8:
                    response.Text("I exclaim ");
                    break;
                case 9:
                    response.Text("I pontificate ");
                    break;
                case 10:
                    response.Text("I know ");
                    break;
                case 11:
                    response.Text("I realize ");
                    break;
                case 12:
                    response.Text("I have come to understand ");
                    break;
                case 13:
                    response.Text("I reached the conclusion ");
                    break;
                case 14:
                    response.Text("Let's summarize ");
                    break;
                case 15:
                    response.Text("So it kind of sums up that ");
                    break;
                case 16:
                    response.Text("I distill ");
                    break;
                case 17:
                    response.Text("I've proven ");
                    mode = 2;
                    break;
                case 18:
                    response.Text("Let me demonstrate my ");
                    mode = 1;
                    break;
                case 19:
                    response.Text("I'm arranging ");
                    mode = 3;
                    break;
                case 20:
                    response.Text("My research found ");
                    mode = 2;
                    break;
            }

            AppendInsult(response, userid, mode);

            messages.SendMessage(response.BuildMessage(), ChannelFlags.Chat | ChannelFlags.Bot, voice);
        }

        void AppendInsult(MessageBuilder response, long userid, int mode=0) {
            if(mode != 1 && mode != 3) {
                response.User(usermodule.GetUser(userid), images);
                switch (RNG.XORShift64.NextInt(5)) {
                    case 0:
                        response.Text("'s mother");
                        break;
                    case 1:
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
                    response.Text($" is {(insult.StartsWithVocal() ? "an" : "a")} ").Text(insult);
                    break;
                case 1:
                    insult = factory.CreateInsult();
                    response.Text(insult).Text(" to ").User(usermodule.GetUser(userid), images);
                    break;
                case 2:
                    insult = factory.CreateInsult();
                    response.Text($" to be {(insult.StartsWithVocal() ? "an" : "a")} ").Text(insult);
                    break;
                case 3:
                    insult = factory.CreateInsult();
                    response.Text($" {(insult.StartsWithVocal() ? "an" : "a")} {insult} for ").User(usermodule.GetUser(userid), images);
                    break;
            }
        }

        long ScanForNames(string message) {
            string[] terms = message.Split(' ').Select(t=>t.Trim('?','!','.',',',';',':')).Where(t => nametriggers.All(n => n != t)).ToArray();
            return usermodule.FindUserIDs(terms).RandomItem(RNG.XORShift64);
        }

        void Greet(string service, string user) {
            MessageBuilder response = new MessageBuilder();
            response.Image(images.GetImageByResource(GetType().Assembly, gangolfpic));
            response.Text("I");

            string welcoming = $"{dictionary.GetRandomWord(WordClass.Verb, WordAttribute.Greeting)}";
            response.Text($" {welcoming.TrimEnd('s')} ");
            response.User(usermodule.GetUser(service, user),images).Text(" the ");

            string insult = factory.CreateInsult();
            response.Text(insult);
            messages.SendMessage(response.BuildMessage(), ChannelFlags.Chat | ChannelFlags.Bot, voice);
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
                CreateInsult(name == 0 ? usermodule.GetUserID(message.Service, message.User) : name);
            }
            else if(explanationtriggers.Any(t => lowermessage.Contains(t)))
                CreateExplanation(usermodule.GetUserID(message.Service, message.User));
            else if(statictriggers.Any(t => lowermessage.Contains(t)))
                CreateInsult(usermodule.GetUserID(message.Service, message.User));
            else {
                if(factory.ContainsInsult(message.Message))
                    if(RNG.XORShift64.NextFloat() < 0.27f)
                        CreateInsult(usermodule.GetUserID(message.Service, message.User));
                    else {
                        lock(chatterlock) {
                            chatternames.TryGetValue(message.User, out int count);
                            chatternames[message.User] = count + 15;
                        }
                    }
            }

            lock (chatterlock)
            {
                chatternames.TryGetValue(message.User, out int count);
                chatternames[message.User] = count + 1;
            }
        }

        /// <summary>
        /// removes a name from the known chatter list
        /// </summary>
        /// <param name="name">name to remove from chatter list</param>
        public void RemoveVictim(string name) {
            lock(chatterlock)
                chatternames.Remove(name);
        }

        void CreateExplanation(long userid) {
            MessageBuilder response = new MessageBuilder();
            response.Image(images.GetImageByResource(GetType().Assembly, gangolfpic));
            switch (RNG.XORShift64.NextInt(2)) {
                case 0:
                    response.Text("It is quite simple. ");
                    break;
                case 1:
                    response.Text("Let me explain that. ");
                    break;
            }
            

            AppendInsult(response, userid);
            messages.SendMessage(response.BuildMessage(), ChannelFlags.Chat | ChannelFlags.Bot, voice);
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

        string GetName(string name) {
            string[] result = name.Split(' ').Select(n => new string(n.Where(char.IsLetter).ToArray())).ToArray();
            switch (RNG.XORShift64.NextInt(3)) {
            case 0: {
                string term = result[0];
                return $"{term.Namify()} {dictionary.GetRandomWord(WordClass.Noun, WordAttribute.Insultive).Text.Namify()}";
            }
            case 1: {
                string term = result[0];
                return $"{term.Namify()} {dictionary.GetRandomWord(WordClass.NameConjuction, WordAttribute.None)} {factory.InsultiveNoun().Namify()}";
            }
            default:
            case 3: {
                string term = result.Length == 1 ? result[0] : result[1];
                return $"{dictionary.GetRandomWord(WordClass.Noun, WordAttribute.Title).Text.Namify()} {term.Namify()}";
            }
            }
        }

        public void Exclaim(string name) {
            name = GetName(name);

            MessageBuilder response = new MessageBuilder();
            response.Image(images.GetImageByResource(GetType().Assembly, gangolfpic));
            if (RNG.XORShift64.NextFloat() < 0.22) {
                Word exclamation = dictionary.GetRandomWord(WordClass.Exclamation, WordAttribute.None);
                if(exclamation != null)
                    response.Text(exclamation.Text.Namify()).Text("! ");
            }

            response.Text(name).Text(" ");

            if(RNG.XORShift64.NextFloat() < 0.3) {
                Word amplifier = dictionary.GetRandomWord(WordClass.Amplifier, WordAttribute.None);
                if(amplifier != null)
                    response.Text(amplifier.Text).Text(" ");
            }

            Word comparision = dictionary.GetRandomWord(WordClass.Verb, WordAttribute.Comparision);
            response.Text(comparision.Text).Text(" like ");
            string insultive = factory.DescriptiveInsultiveNoun();
            response.Text(insultive.StartsWithVocal() ? "an " : "a ");
            response.Text(insultive).Text("!");
            messages.SendMessage(response.BuildMessage(), ChannelFlags.Chat|ChannelFlags.Bot, voice);
        }
    }
}
