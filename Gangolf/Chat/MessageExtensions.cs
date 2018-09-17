namespace NightlyCode.StreamRC.Gangolf.Chat {
    public static class MessageExtensions {

        public static bool StartsWithVocal(this string message) {
            if(string.IsNullOrEmpty(message))
                return false;

            switch (message[0]) {
                case 'A':
                case 'a':
                case 'E':
                case 'e':
                case 'I':
                case 'i':
                case 'O':
                case 'o':
                case 'U':
                case 'u':
                    return true;
            }

            return false;
        }
    }
}