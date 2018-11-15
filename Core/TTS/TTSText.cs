using System.Speech.Synthesis;

namespace StreamRC.Core.TTS {
    public class TTSText {

        /// <summary>
        /// name of voice
        /// </summary>
        public string Voice { get; set; }

        public VoiceGender Gender { get; set; }

        public VoiceAge Age { get; set; }

        public int Index { get; set; } = -1;

        /// <summary>
        /// text to speak
        /// </summary>
        public string Text { get; set; } 
    }
}