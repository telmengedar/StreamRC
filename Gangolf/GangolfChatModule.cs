using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Shops;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace NightlyCode.StreamRC.Gangolf
{

    [Dependency(nameof(RPGMessageModule))]
    [Dependency(nameof(StreamModule))]
    public class GangolfChatModule : IRunnableModule {
        readonly Context context;
        readonly MessageEvaluator messageevaluator=new MessageEvaluator();

        public GangolfChatModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().ChatMessage += OnChatMessage;
        }


        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().ChatMessage -= OnChatMessage;
        }

        void OnChatMessage(ChatMessage message)
        {
            if(messageevaluator.HasInsult(message.Message)) {
                RPGMessageBuilder response = context.GetModule<RPGMessageModule>().Create();
                response.ShopKeeper().Text(" thinks ")
                    .User(context.GetModule<UserModule>().GetUser(message.Service, message.User))
                    .Text("'s mother is a ")
                    .Text(messageevaluator.CreateInsult())
                    .Send();
            }
        }

    }
}
