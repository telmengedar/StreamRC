using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Core.TTS {

    /// <summary>
    /// module used to synthesize text
    /// </summary>
    [ModuleKey("tts")]
    public class TTSModule : ICommandModule,IInitializableModule {
        readonly Context context;

        readonly SpeechSynthesizer synthesizer = new SpeechSynthesizer();

        readonly Queue<TTSText> speechqueue=new Queue<TTSText>();
        bool isspeaking;
        readonly object speaklock = new object();
        string voice;
        string output;

        /// <summary>
        /// creates a new <see cref="TTSModule"/>
        /// </summary>
        public TTSModule(Context context) {
            this.context = context;
            synthesizer.SpeakCompleted += OnSpeakCompleted;
        }

        /// <summary>
        /// triggered when a text is spoken
        /// </summary>
        public event Action<string, string> TextSpoken;

        public string Voice
        {
            get { return voice; }
            set
            {
                if(voice == value)
                    return;

                voice = value;
                context.Settings.Set(this, "voice", voice);
            }
        }

        void OnSpeakCompleted(object sender, SpeakCompletedEventArgs e) {
            TTSText next;
            lock(speaklock) {
                if(speechqueue.Count == 0) {
                    isspeaking = false;
                    return;
                }

                next = speechqueue.Dequeue();
            }

            Thread.Sleep(1000);
            Speak(next);
        }

        void Speak(TTSText text) {
            if (!string.IsNullOrEmpty(text.Voice))
                synthesizer.SelectVoice(text.Voice);
            synthesizer.SpeakAsync(text.Text);
            TextSpoken?.Invoke(text.Voice, text.Text);
        }

        /// <summary>
        /// speaks the specified text
        /// </summary>
        /// <param name="text">text to speak</param>
        public void Speak(string text) {
            string voice = DetectVoice(ref text);
            if(string.IsNullOrEmpty(voice))
                voice = GetVoice("en");

            lock(speaklock) {
                if(isspeaking) {
                    speechqueue.Enqueue(new TTSText {
                        Voice = voice,
                        Text = text
                    });
                    return;
                }

                isspeaking = true;
                Speak(new TTSText {
                    Voice = voice,
                    Text = text
                });
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
            string voice = GetVoice(language);
            if(voice != null)
                text = text.Substring(indexof + 1);
            return voice;
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "streamspeak":
                    if(arguments.Length < 3)
                        throw new Exception("No text specified");
                    Speak(string.Join(" ", arguments.Skip(2)));
                    break;
                case "speak":
                    Speak(string.Join(" ", arguments));
                    break;
                case "voices":
                    Logger.Info(this, "Installed voices", string.Join("\r\n", synthesizer.GetInstalledVoices().Select(v => $"{v.VoiceInfo.Name} ({v.VoiceInfo.Culture.DisplayName}, {v.VoiceInfo.Gender}, {v.VoiceInfo.Age}")));
                    break;
                case "voice":
                    synthesizer.SelectVoice(arguments[0]);
                    Logger.Info(this, "New voice selected", $"{synthesizer.Voice.Name} ({synthesizer.Voice.Culture.DisplayName}, {synthesizer.Voice.Gender}, {synthesizer.Voice.Age})");
                    break;
                default:
                    throw new Exception($"'{command}' not handled by this module");
            }
        }

        void IInitializableModule.Initialize() {
            voice = context.Settings.Get<string>(this, "voice");
        }
    }
}