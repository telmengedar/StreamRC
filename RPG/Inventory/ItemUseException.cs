using System;
using System.Runtime.Serialization;
using StreamRC.RPG.Annotations;

namespace StreamRC.RPG.Inventory {
    public class ItemUseException : Exception {
        public ItemUseException(string message)
            : base(message) {}

        public ItemUseException(string message, Exception innerException)
            : base(message, innerException) {}

        protected ItemUseException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context) {}
    }
}