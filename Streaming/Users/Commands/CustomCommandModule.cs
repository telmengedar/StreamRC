using System.Runtime.Remoting.Contexts;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Scripts;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users.Permissions;

namespace StreamRC.Streaming.Users.Commands {

    /// <summary>
    /// modules providing custom commands to the channel
    /// </summary>
    [Module(AutoCreate = true)]
    public class CustomCommandModule {
        readonly DatabaseModule database;
        readonly StreamModule stream;
        readonly UserPermissionModule permissions;
        readonly ScriptModule scripts;

        /// <summary>
        /// creates a new <see cref="CustomCommandModule"/>
        /// </summary>
        /// <param name="database">access to database</param>
        public CustomCommandModule(DatabaseModule database, StreamModule stream, UserPermissionModule permissions, ScriptModule scripts) {
            this.database = database;
            this.stream = stream;
            this.permissions = permissions;
            this.scripts = scripts;
            database.Database.UpdateSchema<CustomCommand>();
            foreach (CustomCommand command in database.Database.LoadEntities<CustomCommand>().Execute())
                AddCommand(command);
            stream.RegisterCommandHandler("command", new CreateCustomCommandHandler(this, permissions));
        }

        /// <summary>
        /// creates a custom command to be used in chat
        /// </summary>
        /// <param name="chatcommand">name of command in chat</param>
        /// <param name="servicecommand">command to be executed using the <see cref="Context"/></param>
        /// <param name="permissions">permissions to check before executing the command</param>
        /// <returns></returns>
        public CustomCommand CreateCommand(string chatcommand, string servicecommand, string permissions) {
            CustomCommand customcommand = new CustomCommand
            {
                ChatCommand = chatcommand,
                SystemCommand = servicecommand,
                Permissions = permissions
            };

            if (database.Database.Update<CustomCommand>()
                    .Set(c => c.SystemCommand == customcommand.SystemCommand, c => c.Permissions == customcommand.Permissions)
                    .Where(c => c.ChatCommand == customcommand.ChatCommand)
                    .Execute() == 0)
                database.Database.Insert<CustomCommand>().Columns(c => c.ChatCommand, c => c.SystemCommand, c => c.Permissions).Values(customcommand.ChatCommand, customcommand.SystemCommand, customcommand.Permissions).Execute();

            return customcommand;
        }

        /// <summary>
        /// registers a command at the <see cref="StreamModule"/>
        /// </summary>
        /// <param name="command">command to be registered</param>
        public void AddCommand(CustomCommand command) {
            stream.RegisterCommandHandler(command.ChatCommand, new CustomCommandHandler(scripts, permissions, command));
        }

        /// <summary>
        /// unregisters a command from <see cref="StreamModule"/>
        /// </summary>
        /// <param name="command">command to be unregistered</param>
        public void RemoveCommand(string command) {
            stream.UnregisterCommandHandler(command);
        }
    }
}