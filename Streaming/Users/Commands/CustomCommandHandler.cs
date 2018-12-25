using System.Linq;
using StreamRC.Core.Scripts;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;
using StreamRC.Streaming.Users.Permissions;

namespace StreamRC.Streaming.Users.Commands {

    /// <summary>
    /// handles the execution of custom commands in stream
    /// </summary>
    public class CustomCommandHandler : StreamCommandHandler {
        readonly ScriptModule scripts;
        readonly UserPermissionModule permissions;
        readonly CustomCommand customcommand;

        /// <summary>
        /// creates a new <see cref="CustomCommandHandler"/>
        /// </summary>
        /// <param name="scripts">access to scripts</param>
        /// <param name="permissions">access to user permissions</param>
        /// <param name="customcommand">command to be executed</param>
        public CustomCommandHandler(ScriptModule scripts, UserPermissionModule permissions, CustomCommand customcommand) {
            this.scripts = scripts;
            this.permissions = permissions;
            this.customcommand = customcommand;
        }

        /// <inheritdoc />
        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            if(!string.IsNullOrEmpty(customcommand.Permissions)) {
                foreach(string permission in customcommand.Permissions.Split(','))
                    if(!permissions.HasPermission(command.Service, command.User, permission)) {
                        SendMessage(channel, command.User, $"Sorry, you don't have the permissions '{permission}' needed to execute that shit.");
                        return;
                    }
            }

            object result = scripts.Execute(string.Format(customcommand.SystemCommand, command.Arguments.Cast<object>().ToArray()));
            if(result != null)
                SendMessage(channel, command.User, $"{result}");
        }

        /// <inheritdoc />
        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}