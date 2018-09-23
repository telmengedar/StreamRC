using System.Linq;
using NightlyCode.Core.Randoms;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Gangolf.Chat;
using NightlyCode.StreamRC.Modules;
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
    public class GangolfChatModule : IRunnableModule {
        readonly Context context;
        ChatFactory factory;

        readonly string[] statictriggers = {
            "sorry", "sory", "gangolf", "gasgolf", "gamgolf"
        };

        /// <summary>
        /// creates a new <see cref="GangolfChatModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public GangolfChatModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            factory = new ChatFactory();
            context.GetModule<StreamModule>().ChatMessage += OnChatMessage;
        }


        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().ChatMessage -= OnChatMessage;
        }

        void CreateFreeInsult(string service, string user) {
            RPGMessageBuilder response = context.GetModule<RPGMessageModule>().Create();
            response.ShopKeeper();

            int mode = 0;

            switch(RNG.XORShift64.NextInt(21)) {
                case 0:
                    response.Text(" points out that ");
                    break;
                case 1:
                    response.Text(" thinks ");
                    break;
                case 2:
                    response.Text(" imagines ");
                    break;
                case 3:
                    response.Text(" declares ");
                    break;
                case 4:
                    response.Text(" says ");
                    break;
                case 5:
                    response.Text(" feels like ");
                    break;
                case 6:
                    response.Text(" exposes his ");
                    mode = 1;
                    break;
                case 7:
                    response.Text(" decides ");
                    break;
                case 8:
                    response.Text(" exclaims ");
                    break;
                case 9:
                    response.Text(" pontificates ");
                    break;
                case 10:
                    response.Text(" knows ");
                    break;
                case 11:
                    response.Text(" realizes ");
                    break;
                case 12:
                    response.Text(" has come to understand ");
                    break;
                case 13:
                    response.Text(" reached the conclusion ");
                    break;
                case 14:
                    response.Text(" summarizes ");
                    break;
                case 15:
                    response.Text(" sums up that ");
                    break;
                case 16:
                    response.Text(" distills ");
                    break;
                case 17:
                    response.Text(" has proven  ");
                    mode = 2;
                    break;
                case 18:
                    response.Text(" demonstrates his ");
                    mode = 1;
                    break;
                case 19:
                    response.Text(" arranges a ");
                    mode = 3;
                    break;
                case 20:
                    response.Text(" research finds ");
                    mode = 2;
                    break;
            }


            if(mode != 1 && mode != 3) {
                response.User(context.GetModule<UserModule>().GetUser(service, user));
                switch(RNG.XORShift64.NextInt(5)) {
                    case 0:
                        response.Text("'s mother");
                        break;
                    case 1:
                        response.Text("'s father");
                        break;
                }
            }

            string insult;
            switch (mode) {
                default:
                case 0:
                    insult = factory.CreateInsult();
                    response.Text($" is {(insult.StartsWithVocal() ? "an" : "a")} ").Text(insult);
                    break;
                case 1:
                    response.Text(factory.CreateInsult()).Text(" to ").User(context.GetModule<UserModule>().GetUser(service, user));
                    break;
                case 2:
                    insult = factory.CreateInsult();
                    response.Text($" to be {(insult.StartsWithVocal() ? "an" : "a")} ").Text(insult);
                    break;
                case 3:
                    response.Text(factory.CreateInsult()).Text(" for ").User(context.GetModule<UserModule>().GetUser(service, user));
                    break;
            }


            response.Send();
        }

        void OnChatMessage(ChatMessage message) {
            string lowermessage = message.Message.ToLower();
            if(statictriggers.Any(t => lowermessage.Contains(t)) || RNG.XORShift64.NextFloat() < 0.27f && factory.ContainsInsult(message.Message))
                CreateFreeInsult(message.Service, message.User);
        }
    }
}
