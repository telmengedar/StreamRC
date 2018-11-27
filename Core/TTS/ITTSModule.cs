using System;

namespace StreamRC.Core.TTS {
    /// <summary>
    /// interface for a module used to speak text
    /// </summary>
    public interface ITTSModule {

        /// <summary>
        /// triggered when a text is spoken
        /// </summary>
        event Action<string, string> TextSpoken;

        /// <summary>
        /// speaks a text using the tts engine and the specified voice
        /// </summary>
        /// <param name="text">text to speak</param>
        /// <param name="voicename">name of voice to use</param>
        void Speak(string text, string voicename);

        /// <summary>
        /// lists names voices which can be used for speaking text
        /// </summary>
        /// <returns></returns>
        string[] ListVoices();
    }
}