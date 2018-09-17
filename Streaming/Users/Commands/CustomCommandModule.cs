using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users.Permissions;

namespace StreamRC.Streaming.Users.Commands {

    /// <summary>
    /// modules providing custom commands to the channel
    /// </summary>
    [Dependency(nameof(UserModule))]
    [Dependency(nameof(UserPermissionModule))]
    [Dependency(nameof(StreamModule))]
    public class CustomCommandModule : IInitializableModule, IRunnableModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="CustomCommandModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public CustomCommandModule(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            context.Database.Create<CustomCommand>();
        }

        void IRunnableModule.Start() {
            foreach(CustomCommand command in context.Database.LoadEntities<CustomCommand>().Execute())
                AddCommand(command);
            context.GetModule<StreamModule>().RegisterCommandHandler("command", new CreateCustomCommandHandler(this, context.GetModule<UserPermissionModule>()));
        }

        void IRunnableModule.Stop() {
            foreach(CustomCommand command in context.Database.LoadEntities<CustomCommand>().Execute())
                RemoveCommand(command.ChatCommand);
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

            if (context.Database.Update<CustomCommand>()
                    .Set(c => c.SystemCommand == customcommand.SystemCommand, c => c.Permissions == customcommand.Permissions)
                    .Where(c => c.ChatCommand == customcommand.ChatCommand)
                    .Execute() == 0)
                context.Database.Insert<CustomCommand>().Columns(c => c.ChatCommand, c => c.SystemCommand, c => c.Permissions).Values(customcommand.ChatCommand, customcommand.SystemCommand, customcommand.Permissions).Execute();

            return customcommand;
        }

        /// <summary>
        /// registers a command at the <see cref="StreamModule"/>
        /// </summary>
        /// <param name="command">command to be registered</param>
        public void AddCommand(CustomCommand command) {
            context.GetModule<StreamModule>().RegisterCommandHandler(command.ChatCommand, new CustomCommandHandler(context, command));
        }

        /// <summary>
        /// unregisters a command from <see cref="StreamModule"/>
        /// </summary>
        /// <param name="command">command to be unregistered</param>
        public void RemoveCommand(string command) {
            context.GetModule<StreamModule>().UnregisterCommandHandler(command);
        }
    }
}