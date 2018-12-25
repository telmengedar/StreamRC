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
using StreamRC.RPG.Players;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace NightlyCode.StreamRC.Gangolf
{


    /// <summary>
    /// module providing gangolf interaction with chat
    /// </summary>
    [Module(AutoCreate = true)]
    public class GangolfChatModule {
        
        const string gangolfpic = "NightlyCode.StreamRC.Gangolf.Resources.shopkeeper_normal.png";

        readonly ChatFactory factory;
        
        readonly UserModule usermodule;
        readonly ImageCacheModule images;
        readonly DictionaryModule dictionary;
        readonly GangolfSpeakerModule gangolfspeaker;
        readonly PlayerModule playermodule;

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

        /// <summary>
        /// creates a new <see cref="GangolfChatModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        /// <param name="gangolfspeaker"></param>
        public GangolfChatModule(ChatFactory factory, StreamModule stream, ITimerModule timer, UserModule usermodule, ImageCacheModule images, DictionaryModule dictionary, GangolfSpeakerModule gangolfspeaker, PlayerModule playermodule) {
            this.factory = factory;
            this.usermodule = usermodule;
            this.images = images;
            this.dictionary = dictionary;
            this.gangolfspeaker = gangolfspeaker;
            this.playermodule = playermodule;
            stream.ChatMessage += OnChatMessage;
            stream.UserLeft += OnUserLeft;
        }

        void OnUserLeft(UserInformation user) {
            greeted.Remove(new Tuple<string, string>(user.Service, user.Username));
        }

        void CreateInsult(long userid) {
            if(RNG.XORShift64.NextFloat() < 0.5f) {
                FoodHint(userid);
                return;
            }

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
            gangolfspeaker.Speak(userid, response.BuildMessage());
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

            Insult insult;
            switch (mode)
            {
                default:
                case 0:
                    insult = factory.CreateInsult();
                    if (insult.Noun.Attributes.HasFlag(WordAttribute.Countable))
                        response.Text($" is {(insult.Text.StartsWithVocal() ? "an" : "a")} ").Text(insult.Text);
                    else response.Text($" is like ").Text(insult.Text);
                    break;
                case 1:
                    insult = factory.CreateInsult();
                    response.Text(insult.Text).Text(" to ").User(usermodule.GetUser(userid), images);
                    break;
                case 2:
                    insult = factory.CreateInsult();
                    if(insult.Noun.Attributes.HasFlag(WordAttribute.Countable))
                        response.Text($" to be {(insult.Text.StartsWithVocal() ? "an" : "a")} ").Text(insult.Text);
                    else response.Text(" to be like ").Text(insult.Text);
                    break;
                case 3:
                    insult = factory.CreateInsult();
                    if (insult.Noun.Attributes.HasFlag(WordAttribute.Countable))
                        response.Text($" {(insult.Text.StartsWithVocal() ? "an" : "a")} {insult} for ").User(usermodule.GetUser(userid), images);
                    else
                        response.Text($" {insult} for ").User(usermodule.GetUser(userid), images);
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

            Insult insult = factory.CreateInsult();
            response.Text(insult.Text);
            gangolfspeaker.Speak(user, response.BuildMessage());
        }

        void OnChatMessage(IChatChannel channel, ChatMessage message) {
            if(playermodule.GetExistingPlayer(message.Service, message.User)?.Level < 6)
                return;

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
            }
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
            gangolfspeaker.Speak(userid, response.BuildMessage());
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

        public void Describe(string name) {
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
            gangolfspeaker.Speak(name, response.BuildMessage());
        }

        public void FoodHint(long userid) {
            FoodHint(usermodule.GetUser(userid).Name);
        }

        /// <summary>
        /// creates a food hint
        /// </summary>
        /// <param name="name">name for whom to create a food hint</param>
        public void FoodHint(string name) {
            FoodHint(name, false);
        }

        /// <summary>
        /// creates a food hint
        /// </summary>
        /// <param name="name">name for whom to create a food hint</param>
        /// <param name="immediately">whether to talk immediately</param>
        public void FoodHint(string name, bool immediately) {
            name = GetName(name);

            MessageBuilder response = new MessageBuilder();
            response.Image(images.GetImageByResource(GetType().Assembly, gangolfpic));

            if (RNG.XORShift64.NextFloat() < 0.35)
            {
                Word exclamation = dictionary.GetRandomWord(WordClass.Exclamation, WordAttribute.None);
                if (exclamation != null)
                    response.Text(exclamation.Text.Namify()).Text("! ");
            }

            response.Text(name).Text(" ");
            response.Text(dictionary.GetRandomWord(WordClass.Modal | WordClass.Verb, WordAttribute.None).Text);
            response.Text(" ").Text(dictionary.GetRandomWord(WordClass.Declaration | WordClass.Verb, WordAttribute.None).Text).Text(" ");

            WordAttribute type;
            if(RNG.XORShift64.NextFloat() < 0.5)
                type = WordAttribute.Food;
            else type = WordAttribute.Drink;

            response.Text(dictionary.GetRandomWord(WordClass.Verb, type).Text);
            response.Text(" ");
            factory.CreateDish(response, type);

            if(RNG.XORShift64.NextFloat() < 0.31) {
                response.Text(" for ");
                response.Text(dictionary.GetRandomWord(WordClass.Noun, WordAttribute.Event).Text);
            }

            response.Text(".");

            if(immediately)
                gangolfspeaker.SpeakImmediately(response.BuildMessage());
            else gangolfspeaker.Speak(name, response.BuildMessage());
        }
    }
}
