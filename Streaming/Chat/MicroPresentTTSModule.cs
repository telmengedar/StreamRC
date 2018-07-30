using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.TTS;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Chat {

    /// <summary>
    /// sends text of micropresents to tts module if certain conditions are met
    /// </summary>
    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(TTSModule))]
    public class MicroPresentTTSModule : IRunnableModule, IInitializableModule {
        readonly Context context;
        int threshold;

        /// <summary>
        /// creates a new <see cref="MicroPresentTTSModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public MicroPresentTTSModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().MicroPresent += OnPresent;
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().MicroPresent -= OnPresent;
        }

        /// <summary>
        /// amount of to be presented for message to be spoken
        /// </summary>
        public int Threshold
        {
            get { return threshold; }
            set
            {
                if(threshold == value)
                    return;

                threshold = value;
                context.Settings.Set(this, "threshold", threshold);
            }
        }

        void OnPresent(MicroPresent present) {
            if(string.IsNullOrEmpty(present.Message) || present.Amount < Threshold)
                return;

            string text = present.Message.Replace("cheer50", "").Replace("cheer100", "").Replace("cheer250", "").Replace("cheer500", "").Trim();
            if(!string.IsNullOrEmpty(text))
                context.GetModule<TTSModule>().Speak(text);
        }

        void IInitializableModule.Initialize() {
            threshold = context.Settings.Get(this, "threshold", 50);
        }
    }
}