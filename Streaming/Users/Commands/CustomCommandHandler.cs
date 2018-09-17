using System.Linq;
using NightlyCode.StreamRC.Modules;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;
using StreamRC.Streaming.Users.Permissions;

namespace StreamRC.Streaming.Users.Commands {

    /// <summary>
    /// handles the execution of custom commands in stream
    /// </summary>
    public class CustomCommandHandler : StreamCommandHandler {
        readonly Context context;
        readonly CustomCommand customcommand;

        /// <summary>
        /// creates a new <see cref="CustomCommandHandler"/>
        /// </summary>
        /// <param name="context">access to modules (used to execute the command)</param>
        /// <param name="customcommand">command to be executed</param>
        public CustomCommandHandler(Context context, CustomCommand customcommand) {
            this.customcommand = customcommand;
            this.context = context;
        }

        /// <inheritdoc />
        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            if(!string.IsNullOrEmpty(customcommand.Permissions)) {
                foreach(string permission in customcommand.Permissions.Split(','))
                    if(!context.GetModule<UserPermissionModule>().HasPermission(command.Service, command.User, permission)) {
                        SendMessage(channel, command.User, $"Sorry, you don't have the permissions '{permission}' needed to execute that shit.");
                        return;
                    }
            }

            context.ExecuteCommand(string.Format(customcommand.SystemCommand, command.Arguments.Cast<object>().ToArray()).Replace("$Service", command.Service));
            SendMessage(channel, command.User, "Command executed");
        }

        /// <inheritdoc />
        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Executes a custom command (yeah, we have like no detailed help text here)");
        }

        /// <inheritdoc />
        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}