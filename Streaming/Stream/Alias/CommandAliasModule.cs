using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Stream.Alias {

    /// <summary>
    /// module which handles aliases for commands
    /// </summary>
    [Dependency(nameof(StreamModule), SpecifierType.Type)]
    [Module(Key="alias")]
    public class CommandAliasModule : IRunnableModule, IStreamCommandHandler, IInitializableModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="CommandAliasModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public CommandAliasModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// adds an alias to the handled alias list
        /// </summary>
        /// <param name="alias">alias used to trigger a command</param>
        /// <param name="command">command to be executed</param>
        public void Add(string alias, string command) {
            if(context.GetModule<StreamModule>().HasCommandHandler(alias))
                throw new Exception("Alias already in use");

            context.Database.Insert<CommandAlias>().Columns(a => a.Alias, a => a.Command).Values(alias, command).Execute();
            context.GetModule<StreamModule>().RegisterCommandHandler(alias, this);
            Logger.Info(this, $"Alias '{alias}' -> '{command}' added");
        }

        /// <summary>
        /// removes an alias from the handled alias list
        /// </summary>
        /// <param name="alias">name of alias to be removed</param>
        public void Remove(string alias) {
            if(context.Database.Delete<CommandAlias>().Where(a => a.Alias == alias).Execute() > 0) {
                context.GetModule<StreamModule>().UnregisterCommandHandler(alias);
                Logger.Info(this, $"Alias '{alias}' removed");
            }
            else Logger.Info(this, $"Alias '{alias}' not found");
        }

        void IRunnableModule.Start() {
            foreach(CommandAlias alias in context.Database.LoadEntities<CommandAlias>().Execute())
                context.GetModule<StreamModule>().RegisterCommandHandler(alias.Alias, this);
        }

        void IRunnableModule.Stop() {
            foreach(CommandAlias alias in context.Database.LoadEntities<CommandAlias>().Execute())
                context.GetModule<StreamModule>().UnregisterCommandHandler(alias.Alias);
        }

        void IStreamCommandHandler.ExecuteCommand(IChatChannel channel, StreamCommand command) {
            CommandAlias alias = context.Database.LoadEntities<CommandAlias>().Where(a => a.Alias == command.Command).Execute().FirstOrDefault();
            if(alias==null)
                throw new StreamCommandException($"{command.Command} not handled by this module");

            string[] split = alias.Command.Split(' ');
            context.GetModule<StreamModule>().ExecuteCommand(channel, new StreamCommand {
                Service = command.Service,
                Channel = command.Channel,
                User = command.User,
                Command = split[0],
                Arguments = split.Skip(1).ToArray(),
                IsWhispered = command.IsWhispered
            });
        }

        void IStreamCommandHandler.ProvideHelp(IChatChannel channel, string user) {
        }

        public ChannelFlags RequiredFlags => ChannelFlags.None;

        void IInitializableModule.Initialize() {
            context.Database.Create<CommandAlias>();
        }
    }
}