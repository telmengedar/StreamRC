using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Gangolf.Chat;
using NightlyCode.StreamRC.Gangolf.Dictionary;
using StreamRC.Core.Messages;
using StreamRC.Core.Timer;
using StreamRC.Core.TTS;
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
        readonly TTSModule tts;
        readonly UserModule usermodule;
        readonly ImageCacheModule images;

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
        public GangolfChatModule(ChatFactory factory, StreamModule stream, TimerModule timer, ChatMessageModule messages, TTSModule tts, UserModule usermodule, ImageCacheModule images) {
            this.factory = factory;
            this.messages = messages;
            this.tts = tts;
            this.usermodule = usermodule;
            this.images = images;
            stream.ChatMessage += OnChatMessage;
            stream.UserLeft += OnUserLeft;
            timer.AddService(this, 60.0);
        }

        void OnUserLeft(UserInformation user) {
            greeted.Remove(new Tuple<string, string>(user.Service, user.Username));
        }

        void CreateInsult(long userid) {
            StringBuilder speech = new StringBuilder();
            if(RNG.XORShift64.NextFloat() < 0.15)
                speech.Append("Well, ");
            MessageBuilder response = new MessageBuilder();
            response.Image(images.GetImageByResource(GetType().Assembly, gangolfpic));

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
                    speech.Append("My research found ");
                    response.Text(" research finds ");
                    mode = 2;
                    break;
            }

            AppendInsult(response, speech, userid, mode);

            messages.SendMessage(response.BuildMessage(), ChannelFlags.Game);
            tts.Speak(speech.ToString(), voice);
        }

        void AppendInsult(MessageBuilder response, StringBuilder speech, long userid, int mode=0) {
            if(mode != 1 && mode != 3) {
                speech.Append(usermodule.GetUser(userid).Name);
                response.Image(images.GetImageByUrl(usermodule.GetUser(userid).Avatar));
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
                    speech.Append($"{insult} to {usermodule.GetUser(userid).Name}.");
                    response.Text(insult).Text(" to ").Image(images.GetImageByUrl(usermodule.GetUser(userid).Avatar));
                    break;
                case 2:
                    insult = factory.CreateInsult();
                    speech.Append($" to be {(insult.StartsWithVocal() ? "an" : "a")} {insult}.");
                    response.Text($" to be {(insult.StartsWithVocal() ? "an" : "a")} ").Text(insult);
                    break;
                case 3:
                    insult = factory.CreateInsult();
                    speech.Append($" {(insult.StartsWithVocal() ? "an" : "a")} {insult} for {usermodule.GetUser(userid).Name}.");
                    response.Text($" {(insult.StartsWithVocal() ? "an" : "a")} {insult} for ").Image(images.GetImageByUrl(usermodule.GetUser(userid).Avatar));
                    break;
            }
        }

        long ScanForNames(string message) {
            string[] terms = message.Split(' ').Select(t=>t.Trim('?','!','.',',',';',':')).Where(t => nametriggers.All(n => n != t)).ToArray();
            return usermodule.FindUserIDs(terms).RandomItem(RNG.XORShift64);
        }

        void Greet(string service, string user) {
            StringBuilder speech = new StringBuilder();
            MessageBuilder response = new MessageBuilder();
            response.Image(images.GetImageByResource(GetType().Assembly, gangolfpic));
            speech.Append("I ");

            string welcoming = $"{factory.Dictionary.GetWords(w => w.Class == WordClass.Verb && (w.Attributes & WordAttribute.Greeting) == WordAttribute.Greeting).RandomItem(RNG.XORShift64)}";
            speech.Append(welcoming.TrimEnd('s'));
            response.Text($" {welcoming} ");

            speech.Append(' ').Append(usermodule.GetUser(service, user).Name).Append(", the ");
            response.User(usermodule.GetUser(service, user)).Text(" the ");

            string insult = factory.CreateInsult();
            speech.Append(insult);
            response.Text(insult);

            tts.Speak(speech.ToString(), voice);
            messages.SendMessage(response.BuildMessage(), ChannelFlags.Game);
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
            else if(reasontriggers.Any(t => lowermessage.Contains(t))) {
                CreateExplanation(usermodule.GetUserID(message.Service, message.User));
            }
            else if(explanationtriggers.Any(t => lowermessage.Contains(t)))
                Explain(usermodule.GetUserID(message.Service, message.User));
            else if(statictriggers.Any(t => lowermessage.Contains(t)))
                CreateInsult(usermodule.GetUserID(message.Service, message.User));
            else {
                if(factory.ContainsInsult(message.Message))
                    if(RNG.XORShift64.NextFloat() < 0.27f)
                        CreateInsult(usermodule.GetUserID(message.Service, message.User));
                    else {
                        lock(chatterlock) {
                            chatternames.TryGetValue(message.User, out int count);
                            chatternames[message.User] = count + 1;
                        }
                    }
            }
        }

        void CreateExplanation(long userid) {
            MessageBuilder response = new MessageBuilder();
            response.Image(images.GetImageByResource(GetType().Assembly, gangolfpic));
            response.Text(" says it's quite simple. ");

            StringBuilder speech = new StringBuilder("It is quite simple. ");
            AppendInsult(response, speech, userid);
            messages.SendMessage(response.BuildMessage(), ChannelFlags.Game);
            tts.Speak(speech.ToString(), voice);
        }

        void Explain(long userid) {
            MessageBuilder response = new MessageBuilder();
            response.Image(images.GetImageByResource(GetType().Assembly, gangolfpic));
            response.Text(" explains that ");

            StringBuilder speech = new StringBuilder("Let me explain that. ");
            AppendInsult(response, speech, userid);
            messages.SendMessage(response.BuildMessage(), ChannelFlags.Game);
            tts.Speak(speech.ToString(), voice);
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
            tts.Speak(sb.ToString(), voice);
        }
    }
}
