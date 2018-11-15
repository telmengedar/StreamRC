using System;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Stream.Alias {

    /// <summary>
    /// module which handles aliases for commands
    /// </summary>
    [Module(Key="alias")]
    public class CommandAliasModule : IStreamCommandHandler {
        readonly DatabaseModule database;
        readonly StreamModule stream;

        /// <summary>
        /// creates a new <see cref="CommandAliasModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public CommandAliasModule(DatabaseModule database, StreamModule stream) {
            this.database = database;
            this.stream = stream;
            database.Database.UpdateSchema<CommandAlias>();
            foreach (CommandAlias alias in database.Database.LoadEntities<CommandAlias>().Execute())
                stream.RegisterCommandHandler(alias.Alias, this);
        }

        /// <summary>
        /// adds an alias to the handled alias list
        /// </summary>
        /// <param name="alias">alias used to trigger a command</param>
        /// <param name="command">command to be executed</param>
        public void Add(string alias, string command) {
            if(stream.HasCommandHandler(alias))
                throw new Exception("Alias already in use");

            database.Database.Insert<CommandAlias>().Columns(a => a.Alias, a => a.Command).Values(alias, command).Execute();
            stream.RegisterCommandHandler(alias, this);
            Logger.Info(this, $"Alias '{alias}' -> '{command}' added");
        }

        /// <summary>
        /// removes an alias from the handled alias list
        /// </summary>
        /// <param name="alias">name of alias to be removed</param>
        public void Remove(string alias) {
            if(database.Database.Delete<CommandAlias>().Where(a => a.Alias == alias).Execute() > 0) {
                stream.UnregisterCommandHandler(alias);
                Logger.Info(this, $"Alias '{alias}' removed");
            }
            else Logger.Info(this, $"Alias '{alias}' not found");
        }

        void IStreamCommandHandler.ExecuteCommand(IChatChannel channel, StreamCommand command) {
            CommandAlias alias = database.Database.LoadEntities<CommandAlias>().Where(a => a.Alias == command.Command).Execute().FirstOrDefault();
            if(alias==null)
                throw new StreamCommandException($"{command.Command} not handled by this module");

            string[] split = alias.Command.Split(' ');
            stream.ExecuteCommand(channel, new StreamCommand {
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
    }
}