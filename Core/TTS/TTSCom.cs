using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using SpeechLib;

namespace StreamRC.Core.TTS {

    /// <summary>
    /// com implementation of speech synthesizer
    /// </summary>
    public class TTSCom {
        const SpeechVoiceSpeakFlags speechFlags = SpeechVoiceSpeakFlags.SVSFlagsAsync;

        public int Volume { get; set; } = 100;

        public int Rate { get; set; } = 0;

        public void Speak(string text, string voicename) {

            SpVoice synth = new SpVoice();
            SpMemoryStream wave = new SpMemoryStream();
            ISpeechObjectTokens voices = synth.GetVoices();
            
            try
            {
                // synth setup
                synth.Volume = Math.Max(1, Math.Min(100, Volume));
                synth.Rate = Math.Max(-10, Math.Min(10, Rate));
                synth.Voice = voices.Cast<SpObjectToken>().FirstOrDefault(t => t.GetAttribute("Name") == voicename);
                if(synth.Voice == null)
                    synth.Voice = voices.Cast<SpObjectToken>().FirstOrDefault();

                wave.Format.Type = SpeechAudioFormatType.SAFT22kHz16BitMono;
                //synth.AudioOutputStream = wave;
                synth.Speak(text, speechFlags);
                synth.WaitUntilDone(Timeout.Infinite);


                /*using(var ms = new MemoryStream((byte[])wave.GetData())) {
                    SoundPlayer player = new SoundPlayer(ms);
                    player.PlaySync();
                }*/
            }
            finally
            {
                Marshal.ReleaseComObject(voices);
                Marshal.ReleaseComObject(wave);
                Marshal.ReleaseComObject(synth);
            }
        }
    }
}