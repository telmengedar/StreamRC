using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace NightlyCode.StreamRC.Gangolf
{

    [Dependency(nameof(StreamModule),SpecifierType.Type)]
    public class GangolfChatModule : IRunnableModule {
        readonly Context context;

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
            throw new System.NotImplementedException();
        }

    }
}
