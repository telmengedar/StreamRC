using System;
using System.Collections.Generic;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;

namespace StreamRC.Core.TTS {

    /// <summary>
    /// module used to synthesize text
    /// </summary>
    [Module(Key ="tts")]
    public class TTSModule {
        //readonly SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        TTSCom synthesizer = new TTSCom();

        readonly Queue<TTSText> speechqueue=new Queue<TTSText>();
        bool isspeaking;
        readonly object speaklock = new object();

        /// <summary>
        /// creates a new <see cref="TTSModule"/>
        /// </summary>
        public TTSModule() {
            //synthesizer.SpeakCompleted += OnSpeakCompleted;
        }

        /// <summary>
        /// triggered when a text is spoken
        /// </summary>
        public event Action<string, string> TextSpoken;

        void OnSpeakCompleted() {
            TTSText next;
            lock(speaklock) {
                if(speechqueue.Count == 0) {
                    isspeaking = false;
                    return;
                }

                next = speechqueue.Dequeue();
            }

            Thread.Sleep(1000);
            Task.Run(() => Speak(next));
        }

        void Speak(TTSText text) {
            /*if(!string.IsNullOrEmpty(text.Voice)) {
                try {
                    synthesizer.SelectVoice(text.Voice);
                }
                catch(Exception e) {
                    Logger.Error(this, $"Unable to select voice {text.Voice}", e);
                }
            }
            else if(text.Index >= 0) {
                try {
                    synthesizer.SelectVoiceByHints(text.Gender, text.Age, text.Index);
                }
                catch(Exception e) {
                    Logger.Error(this, $"Unable to select voice {text.Gender}, {text.Age}, {text.Index}", e);
                }
            }*/

            synthesizer.Speak(text.Text, text.Voice);
            //synthesizer.SpeakAsync(text.Text);
            try {
                TextSpoken?.Invoke(text.Voice, text.Text);
            }
            catch(Exception e) {
                Logger.Error(this, "Error triggering text spoken event", e);
            }
            OnSpeakCompleted();
        }

        /// <summary>
        /// speaks the specified text
        /// </summary>
        /// <param name="text">text to speak</param>
        public void Speak(string text) {
            string voicename = DetectVoice(ref text);
            if(string.IsNullOrEmpty(voicename))
                voicename = GetVoice("en");

            Speak(text, voicename);
        }

        /// <summary>
        /// speaks a text using the tts engine and the specified voice
        /// </summary>
        /// <param name="text">text to speak</param>
        /// <param name="voicename">name of voice to use</param>
        public void Speak(string text, string voicename) {
            lock (speaklock)
            {
                if (isspeaking)
                {
                    speechqueue.Enqueue(new TTSText
                    {
                        Voice = voicename,
                        Text = text
                    });
                    return;
                }

                isspeaking = true;
                Task.Run(() => Speak(new TTSText {
                    Voice = voicename,
                    Text = text
                }));
            }
        }

        /// <summary>
        /// speaks a text using the tts engine and the specified voice
        /// </summary>
        /// <param name="text">text to speak</param>
        public void Speak(string text, VoiceGender gender, VoiceAge age, int index=0)
        {
            lock (speaklock) {
                TTSText ttstext = new TTSText {
                    Text = text,
                    Gender = gender,
                    Age = age,
                    Index = index
                };

                if (isspeaking) {
                    speechqueue.Enqueue(ttstext);
                    return;
                }

                isspeaking = true;
                Speak(ttstext);
            }
        }

        string GetVoice(string language) {
            switch(language.ToLower()) {
                case "de":
                case "deutsch":
                    return "Microsoft Hedda Desktop";
                case "en":
                case "english":
                    return "Microsoft Zira Desktop";
            }
            return null;
        }

        string DetectVoice(ref string text) {
            int indexof = text.IndexOf(' ');
            if(indexof == -1)
                return null;

            string language = text.Substring(0, indexof);
            string voicename = GetVoice(language);
            if(voicename != null)
                text = text.Substring(indexof + 1);
            return voicename;
        }

        /// <summary>
        /// lists names voices which can be used for speaking text
        /// </summary>
        /// <returns></returns>
        public string[] ListVoices() {
            //return synthesizer.GetInstalledVoices().Select(v => $"{v.VoiceInfo.Name} ({v.VoiceInfo.Culture.DisplayName}, {v.VoiceInfo.Gender}, {v.VoiceInfo.Age}").ToArray();
            return new string[0];
        }
    }
}