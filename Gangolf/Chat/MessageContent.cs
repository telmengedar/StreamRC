namespace NightlyCode.StreamRC.Gangolf.Chat {
    public class MessageContent {
        public MessageTarget SubjectType { get; set; }
        public string Subject { get; set; }
        public string Predicate { get; set; }
        public string Object { get; set; }
    }
}