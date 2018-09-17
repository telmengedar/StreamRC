using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;
using StreamRC.Streaming.Users.Permissions;

namespace StreamRC.Streaming.Users.Commands {

    /// <summary>
    /// handles 'command' command used to create custom stream commands
    /// </summary>
    public class CreateCustomCommandHandler : StreamCommandHandler {
        readonly CustomCommandModule commandmodule;
        readonly UserPermissionModule permissionmodule;

        /// <summary>
        /// creates a new <see cref="CustomCommandHandler"/>
        /// </summary>
        /// <param name="commandmodule">access to <see cref="CustomCommandModule"/></param>
        /// <param name="permissionmodule">access to user permissions</param>
        public CreateCustomCommandHandler(CustomCommandModule commandmodule, UserPermissionModule permissionmodule) {
            this.commandmodule = commandmodule;
            this.permissionmodule = permissionmodule;
        }

        /// <inheritdoc />
        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            if(!permissionmodule.HasPermission(command.Service, command.User, "command:create")) {
                SendMessage(channel, command.User, "You don't have the required permissions to create custom commands.");
                return;
            }

            if(command.Arguments.Length < 2) {
                SendMessage(channel, command.User, "Syntax: command <name> <command> <permissions>");
                return;
            }

            CustomCommand customcommand = commandmodule.CreateCommand(command.Arguments[0],
                command.Arguments[1],
                command.Arguments.Length > 2 ? command.Arguments[2] : "");
            commandmodule.AddCommand(customcommand);
        }

        /// <inheritdoc />
        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Creates a custom command for chat");
        }

        /// <inheritdoc />
        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}