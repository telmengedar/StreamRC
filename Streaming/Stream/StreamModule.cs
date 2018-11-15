using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Collections;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Scripts;
using StreamRC.Streaming.Extensions;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;
using StreamRC.Streaming.Users;
using StreamRC.Streaming.Users.Permissions;

namespace StreamRC.Streaming.Stream {
    /// <summary>
    /// module providing stream information
    /// </summary>
    [Module(Key="stream")]
    public class StreamModule : IStreamModule, IChatMessageSender, IStreamStatsModule {
        readonly UserPermissionModule permissions;
        readonly ScriptModule scripts;
        readonly UserModule users;
        readonly Dictionary<string, IStreamServiceModule> services = new Dictionary<string, IStreamServiceModule>();
        readonly StreamCommandManager commandmanager = new StreamCommandManager();

        readonly object channellock = new object();
        readonly List<IChatChannel> channels=new List<IChatChannel>();

        /// <summary>
        /// creates a new <see cref="StreamModule"/>
        /// </summary>
        /// <param name="permissions">access to user permissions</param>
        /// <param name="scripts">access to scripts</param>
        /// <param name="users">access to user data</param>
        public StreamModule(UserPermissionModule permissions, ScriptModule scripts, UserModule users) {
            this.permissions = permissions;
            this.scripts = scripts;
            this.users = users;
            RegisterCommandHandler("commands", new CommandListHandler(commandmanager));
            RegisterCommandHandler("uptime", new UptimeCommandHandler());
            RegisterCommandHandler("help", new HelpCommandHandler(commandmanager));
        }

        /// <summary>
        /// get a channel which matches the filters
        /// </summary>
        /// <param name="service">service which manages the channel</param>
        /// <param name="channelname">name of channel</param>
        /// <param name="included">flags channel has to have</param>
        /// <param name="excluded">flags channel mustn't have</param>
        /// <returns>access to channel</returns>
        public IChatChannel GetChannel(string service, string channelname, ChannelFlags included = ChannelFlags.None, ChannelFlags excluded = ChannelFlags.None) {
            lock(channellock)
                return channels.FirstOrDefault(c => c.Service == service && c.Name == channelname && (c.Flags & included) == included && (c.Flags & excluded) == ChannelFlags.None);
        }

        /// <summary>
        /// get a channel which matches the filters
        /// </summary>
        /// <param name="service">service which manages the channel</param>
        /// <param name="included">flags channel has to have</param>
        /// <param name="excluded">flags channel mustn't have</param>
        /// <returns>access to channel</returns>
        public IChatChannel GetChannel(string service, ChannelFlags included=ChannelFlags.All, ChannelFlags excluded=ChannelFlags.None) {
            lock(channellock)
                return channels.FirstOrDefault(c => c.Service == service && (c.Flags & included) == included && (c.Flags & excluded) == ChannelFlags.None);
        }

        /// <summary>
        /// get all channels which are matching the filters
        /// </summary>
        /// <param name="included">flags channel has to have</param>
        /// <param name="excluded">flags channel mustn't have</param>
        /// <returns>channels which match the specified flags</returns>
        public IEnumerable<IChatChannel> GetChannels(ChannelFlags included = ChannelFlags.All, ChannelFlags excluded = ChannelFlags.None) {
            lock(channellock)
                return channels.Where(c => (c.Flags & included) == included && (c.Flags & excluded) == ChannelFlags.None);
        }

        public void AddChannel(IChatChannel channel) {
            Logger.Info(this, $"Adding channel '{channel.Name}' from connected channel list");
            lock (channellock) {
                channel.UserJoined += OnChannelUserJoined;
                channel.UserLeft += OnChannelUserLeft;

                if(channel.Flags.HasFlag(ChannelFlags.Chat) || channel.Flags.HasFlag(ChannelFlags.Command))
                    channel.ChatMessage += OnChannelChatMessage;

                if(channel is IBotChatChannel botchannel)
                    botchannel.CommandReceived += OnChannelCommandReceived;

                if(channel is IChannelInfoChannel infochannel) {
                    infochannel.Hosted += OnHosted;
                    infochannel.Raid += OnRaided;
                    infochannel.MicroPresent += OnChannelMicroPresentReceived;
                }

                channels.Add(channel);
                channel.Initialize();
            }
        }

        /// <summary>
        /// remove all channels which match the specified predicate
        /// </summary>
        /// <param name="predicate">predicate to match</param>
        public void RemoveChannels(Func<IChatChannel, bool> predicate) {
            lock(channellock) {
                foreach(IChatChannel channel in channels.Where(predicate).ToArray())
                    RemoveChannel(channel);
            }
        }

        public void RemoveChannels(string service) {
            lock(channellock) {
                foreach(IChatChannel channel in channels.Where(c => c.Service == service).ToArray())
                    RemoveChannel(channel);
            }
        }

        public void RemoveChannel(string service, string name) {
            lock(channellock) {
                IChatChannel channel = GetChannel(service, name);
                RemoveChannel(channel);
            }
        }

        public void RemoveChannel(IChatChannel channel) {
            Logger.Info(this, $"Removing channel '{channel.Name}' from connected channel list");

            lock(channellock) {
                channel.UserJoined -= OnChannelUserJoined;
                channel.UserLeft -= OnChannelUserLeft;

                if (channel.Flags.HasFlag(ChannelFlags.Chat) || channel.Flags.HasFlag(ChannelFlags.Command))
                    channel.ChatMessage -= OnChannelChatMessage;

                if (channel is IBotChatChannel botchannel)
                {
                    botchannel.CommandReceived -= OnChannelCommandReceived;
                }

                if (channel is IChannelInfoChannel infochannel)
                {
                    infochannel.Hosted -= OnHosted;
                    infochannel.Raid -= OnRaided;
                    infochannel.MicroPresent -= OnChannelMicroPresentReceived;
                }

                channels.Remove(channel);
            }
        }

        void OnChannelMicroPresentReceived(IChannelInfoChannel channel, MicroPresent present)
        {
            Logger.Info(this, $"{present.Service}/{present.Username} donated {present.Amount} {present.Currency}.");
            MicroPresent?.Invoke(present);
        }

        void OnChannelCommandReceived(IBotChatChannel channel, StreamCommand command)
        {
            if (!ValidUser(command.Service, command.User))
                return;

            if(command.IsSystemCommand) {
                if(!permissions.HasPermission(command.Service, command.User, "admin"))
                    SendMessage(command.Service, command.Channel, command.User, "How about you leave the complicated stuff to the big guys?");
                else ExecuteSystemCommand(command);
                return;
            }

            string whispered = command.IsWhispered ? "(Whispered)" : "";
            Logger.Info(this, $"{command.Service} - {whispered}{command.User}: !{command.Command} {string.Join(" ", command.Arguments)}");
            ExecuteCommand(channel, command);
        }

        void ExecuteSystemCommand(StreamCommand command) {
            try {
                object result =  scripts.Execute(command.Command) ?? "Command Executed";
                if(result is Array array)
                    result = string.Join("\n", array.Cast<object>());

                IChatChannel channel = GetChannel(command.Service, command.Channel);
                if(!channel.Flags.HasFlag(ChannelFlags.LineBreaks))
                    result = result.ToString().Replace('\n', ';');

                SendMessage(command.Service, command.Channel, command.User, result.ToString());
            }
            catch(Exception e) {
                Logger.Error(this, $"Unable to execute '{command.Command}'", e);
                SendMessage(command.Service, command.Channel, command.User, "There was an error executing the command.");
            }
        }

        void OnChannelChatMessage(IChatChannel channel, ChatMessage message) {
            if(!ValidUser(message.Service, message.User))
                return;

            string color = users.GetUserColor(message.Service, message.User);
            if(!string.IsNullOrEmpty(color)) {
                message.UserColor = color.ParseColor();
            }
            else {
                User user = users.GetUser(message.Service, message.User);

                color = $"#{message.UserColor.R:X2}{message.UserColor.G:X2}{message.UserColor.B:X2}";
                if (user.Color != color)
                    users.UpdateUserColor(user, color);
            }

            string whispered = message.IsWhisper ? "(Whispered)" : "";
            Logger.Info(this, $"{message.Service} - {whispered}{message.User}: {message.Message}");
            ChatMessage?.Invoke(channel, message);
        }

        void OnChannelUserJoined(IChatChannel channel, UserInformation information) {
            users.UserJoined(information.Service, information.Username);
            UserJoined?.Invoke(information);
        }

        void OnChannelUserLeft(IChatChannel channel, UserInformation information) {
            users.UserLeft(information.Service, information.Username);
            UserLeft?.Invoke(information);
        }

        bool ValidUser(string service, string username) {

            // the user doesn't necessarily exist just yet ... could be a message from an unknown user
            User user = users.GetUser(service, username);
            bool result = (user.Flags & UserFlags.Bot) == UserFlags.None;
            if(!result)
                Logger.Warning(this, $"Blocking message for {service}/{username} because of userflags ({user.Flags})");
            return result;
        }

        public void TriggerHosted(string service, string username, int viewers) {
            OnHosted(null, new HostInformation {
                Service = service,
                Channel = username,
                Viewers = viewers
            });
        }

        void OnRaided(IChannelInfoChannel channel, RaidInformation info)
        {
            Logger.Info(this, $"Channel is being raided by {info.Login} with {info.RaiderCount} users on {info.Service}");
            if(ValidUser(info.Service, info.Login))
                Raid?.Invoke(info);
        }

        void OnHosted(IChannelInfoChannel channel, HostInformation information) {
            Logger.Info(this, $"Channel is being hosted by {information.Channel} with {information.Viewers} viewers on {information.Service}");
            if(ValidUser(information.Service, information.Channel))
                Hosted?.Invoke(information);
        }

        /// <summary>
        /// adds a service to the collection
        /// </summary>
        /// <param name="type">type of service</param>
        /// <param name="module">module handling service calls</param>
        public void AddService(string type, IStreamServiceModule module) {
            services[type] = module;
            module.Connected += () => {
                Logger.Info(this, $"{type} connected");
                ServiceConnected?.Invoke(type);
            };
            module.NewFollower += information => {
                Logger.Info(this, $"New follower '{information.Username}' on {type}.");
                if(ValidUser(information.Service, information.Username)) {
                    users.SetUserStatus(information.Service, information.Username, UserStatus.Follower);
                    NewFollower?.Invoke(information);
                }
            };
            module.NewSubscriber += AddSubscriber;
            if(module is IStreamStatsModule statsModule)
                statsModule.ViewersChanged += OnViewersChanged;
        }

        void OnViewersChanged(int viewers) {
            ViewersChanged?.Invoke(Viewers);
        }

        public void AddSubscriber(SubscriberInformation subscriber) {
            Logger.Info(this, $"New subscriber '{subscriber.Username}' with plan {subscriber.PlanName} on {subscriber.Service}.");
            users.SetUserStatus(subscriber.Service, subscriber.Username, subscriber.Status);
            NewSubscriber?.Invoke(subscriber);
        }

        /// <summary>
        /// triggered when a new follower was detected
        /// </summary>
        public event Action<UserInformation> NewFollower;

        /// <summary>
        /// triggered when a new subscriber was detected
        /// </summary>
        public event Action<SubscriberInformation> NewSubscriber;

        /// <summary>
        /// triggered when channel is being hosted
        /// </summary>
        public event Action<HostInformation> Hosted;

        /// <summary>
        /// triggered when channel is being raided
        /// </summary>
        public event Action<RaidInformation> Raid;

        /// <summary>
        /// triggered when user has joined the channel
        /// </summary>
        public event Action<UserInformation> UserJoined;

        /// <summary>
        /// triggered when user has left the channel
        /// </summary>
        public event Action<UserInformation> UserLeft;

        /// <summary>
        /// triggered when a new service was connected to the module
        /// </summary>
        public event Action<string> ServiceConnected;

        /// <summary>
        /// triggered when a new chat message was received
        /// </summary>
        public event Action<IChatChannel, ChatMessage> ChatMessage;

        /// <summary>
        /// triggered when a command was received
        /// </summary>
        public event Action<StreamCommand> Command;

        /// <summary>
        /// triggered when a micropresent was received
        /// </summary>
        public event Action<MicroPresent> MicroPresent;

        public IStreamServiceModule GetService(string service) {
            return services[service];
        }

        /// <summary>
        /// registers a command handler for a command
        /// </summary>
        /// <param name="command">command for which to register handler</param>
        /// <param name="handler">handler used to process commands</param>
        public void RegisterCommandHandler(string command, IStreamCommandHandler handler) {
            commandmanager.AddCommandHandler(command, handler);
        }

        /// <summary>
        /// unregisters existing command handler
        /// </summary>
        /// <param name="command">command to unregister</param>
        public void UnregisterCommandHandler(string command) {
            commandmanager.RemoveCommandHandler(command);
        }

        /// <summary>
        /// executes a stream command received by a channel
        /// </summary>
        /// <param name="channel">channel the command was received from</param>
        /// <param name="command">received command</param>
        public void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            IStreamCommandHandler handler = commandmanager.GetCommandHandlerOrDefault(command.Command);

            if(handler == null)
                channel.SendMessage($"Unknown command '{command.Command}', try '!commands' for a list of commands.");
            else {
                if((channel.Flags & handler.RequiredFlags) != handler.RequiredFlags)
                    return;

                try {
                    handler.ExecuteCommand(channel, command);
                }
                catch(StreamCommandException e) {
                    channel.SendMessage(e.Message);
                    if(e.ProvideHelp)
                        channel.SendMessage($"Try '!help {command.Command}' to get an idea how to use the command.");

                }
                catch(Exception e) {
                    Logger.Error(this, $"Error processing command '{command}'", e);
                    channel.SendMessage("Error processing command. You should probably inform the streamer that this tool is crap (also he can take a look at the logs what went wrong).");
                }
            }

            Command?.Invoke(command);
        }

        /// <summary>
        /// determines whether a handler for the specified command exists
        /// </summary>
        /// <param name="command">command to be checked</param>
        /// <returns></returns>
        public bool HasCommandHandler(string command) {
            return commandmanager.Commands.Any(c => c == command);
        }

        public void SendMessage(string service, string channel, string user, string message) {
            GetChannel(service, channel)?.SendMessage($"{user}: {message}");
        }

        void IChatMessageSender.SendChatMessage(string message) {
            GetChannels(ChannelFlags.Chat, ChannelFlags.Bot).Foreach(c => c.SendMessage(message));
        }

        public event Action<int> ViewersChanged;

        public int Viewers
        {
            get { return services.Where(s => s.Value is IStreamStatsModule).Select(s=>s.Value).Cast<IStreamStatsModule>().Sum(s => s.Viewers); }
        }
    }
}