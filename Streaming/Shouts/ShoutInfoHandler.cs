using System.Linq;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Shouts {
    public class ShoutInfoHandler : StreamCommandHandler {
        readonly ShoutModule module;

        public ShoutInfoHandler(ShoutModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            if(command.Arguments.Length == 0) {
                SendMessage(channel, command.User, "You should at least tell me which shout you are interested in.");
                return;
            }

            string term = string.Join(" ", command.Arguments);
            Shout shout = module.Shouts.FirstOrDefault(s => s.Term == term);
            if(shout == null) {
                SendMessage(channel, command.User, $"Whatever {term} is, it's totally not a shout.");
                return;
            }

            SendMessage(channel, command.User, $"Term: {shout.Term}: Id: {shout.VideoId}, Cooldown: {shout.Cooldown}, Start: {shout.StartSeconds}, End: {shout.EndSeconds}, Volume: {shout.Volume}");
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Sends info about a shout to chat");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}