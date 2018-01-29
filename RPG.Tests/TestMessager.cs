using System;
using StreamRC.Core.Messages;

namespace RPG.Tests {
    public class TestMessager : IMessageModule {
        public event Action<Message> Message;

        public void AddMessage(Message message) {
        }
    }
}