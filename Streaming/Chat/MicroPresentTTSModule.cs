using NightlyCode.Modules;
using StreamRC.Core.Settings;
using StreamRC.Core.TTS;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Chat {

    /// <summary>
    /// sends text of micropresents to tts module if certain conditions are met
    /// </summary>
    [Module(AutoCreate = true)]
    public class MicroPresentTTSModule {
        readonly ISettings settings;
        readonly TTSModule tts;
        int threshold;

        /// <summary>
        /// creates a new <see cref="MicroPresentTTSModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public MicroPresentTTSModule(StreamModule stream, ISettings settings, TTSModule tts) {
            this.settings = settings;
            this.tts = tts;
            threshold = settings.Get(this, "threshold", 50);
            stream.MicroPresent += OnPresent;
        }

        /// <summary>
        /// amount of to be presented for message to be spoken
        /// </summary>
        public int Threshold
        {
            get => threshold;
            set
            {
                if(threshold == value)
                    return;

                threshold = value;
                settings.Set(this, "threshold", threshold);
            }
        }

        void OnPresent(MicroPresent present) {
            if(string.IsNullOrEmpty(present.Message) || present.Amount < Threshold)
                return;

            string text = present.Message.Replace("cheer50", "").Replace("cheer100", "").Replace("cheer250", "").Replace("cheer500", "").Trim();
            if(!string.IsNullOrEmpty(text))
                tts.Speak(text);
        }
    }
}