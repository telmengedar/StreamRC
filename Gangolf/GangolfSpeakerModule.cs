using System;
using System.Collections.Generic;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using NightlyCode.Modules;
using StreamRC.Core.Messages;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Chat;
using StreamRC.Streaming.Stream.Chat;

namespace NightlyCode.StreamRC.Gangolf {

    [Module(Key = "gangolf")]
    public class GangolfSpeakerModule : ITimerService {
        const string voice = "CereVoice Stuart - English (Scotland)";
        readonly object messagelock = new object();
        readonly List<Message> messages = new List<Message>();

        readonly ChatMessageModule chatmessages;

        DateTime nexttrigger = DateTime.MinValue;

        /// <summary>
        /// creates a new <see cref="GangolfSpeakerModule"/>
        /// </summary>
        /// <param name="timer">access to timer table</param>
        /// <param name="chatmessages">access to stream messages</param>
        public GangolfSpeakerModule(ITimerModule timer, ChatMessageModule chatmessages) {
            this.chatmessages = chatmessages;
            timer.AddService(this, 30.0);
        }

        /// <summary>
        /// let gangolf talk something
        /// </summary>
        /// <param name="message">message to talk</param>
        public void Speak(Message message) {
            lock(messagelock)
                messages.Add(message);
        }

        /// <summary>
        /// let gangolf speak a text as soon as possible
        /// </summary>
        /// <param name="message"></param>
        public void SpeakImmediately(Message message) {
            chatmessages.SendMessage(message, ChannelFlags.Bot, voice);
        }

        void ITimerService.Process(double time) {
            if(DateTime.Now < nexttrigger)
                return;

            lock(messagelock) {
                if(messages.Count == 0)
                    return;

                Message message = messages.RandomItem(RNG.XORShift64);
                messages.Remove(message);
                chatmessages.SendMessage(message, ChannelFlags.Bot, voice);
            }

            nexttrigger = DateTime.Now + TimeSpan.FromSeconds(30.0 + RNG.XORShift64.NextDouble() * 60.0);
        }
    }
}