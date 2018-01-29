using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Helpers;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Streaming.Stream {
    /// <summary>
    /// module providing stream information
    /// </summary>
    [ModuleKey("stream")]
    public class StreamModule : IRunnableModule, IStreamCommandHandler, IMessageSenderModule, ICommandModule, IStreamModule {
        readonly Context context;
        readonly object commandhandlerlock = new object();
        readonly Dictionary<string, IStreamCommandHandler> commandhandlers = new Dictionary<string, IStreamCommandHandler>();
        readonly Dictionary<string, IStreamServiceModule> services = new Dictionary<string, IStreamServiceModule>();

        DateTime start = DateTime.Now;

        /// <summary>
        /// creates a new <see cref="StreamModule"/>
        /// </summary>
        /// <param name="context">context used to access environment</param>
        public StreamModule(Context context) {
            this.context = context;
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
                NewFollower?.Invoke(information);
            };
            module.NewSubscriber += information => {
                Logger.Info(this, $"New subscriber '{information.Username}' with plan {information.PlanName} on {type}.");
                NewSubscriber?.Invoke(information);
            };
            module.Hosted += information => {
                Logger.Info(this, $"Channel is being hosted by {information.Channel} with {information.Viewers} viewers on {type}");
                Hosted?.Invoke(information);
            };
            module.UserJoined += information => {
                UserJoined?.Invoke(information);
            };
            module.ChatMessage += message => {
                string whispered = message.IsWhisper ? "(Whispered)" : "";
                Logger.Info(this, $"{type} - {whispered}{message.User}: {message.Message}");
                ChatMessage?.Invoke(message);
            };
            module.UserLeft += information => UserLeft?.Invoke(information);
            module.CommandReceived += command => {
                string whispered = command.IsWhispered ? "(Whispered)" : "";
                Logger.Info(this, $"{type} - {whispered}{command.User}: !{command.Command} {string.Join(" ", command.Arguments)}");
                OnCommand(command);
            };

            module.MicroPresent += present => {
                Logger.Info(this, $"{present.Service}/{present.Username} donated {present.Amount} {present.Currency}.");
                MicroPresent?.Invoke(present);
            };
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
        public event Action<ChatMessage> ChatMessage;

        /// <summary>
        /// triggered when a command was received
        /// </summary>
        public event Action<StreamCommand> Command;

        /// <summary>
        /// triggered when a micropresent was received
        /// </summary>
        public event Action<MicroPresent> MicroPresent;

        void IRunnableModule.Start() {
            RegisterCommandHandler(this, "commands", "uptime", "help");
        }

        public IStreamServiceModule GetService(string service) {
            return services[service];
        }

        /// <summary>
        /// registers a command handler for a command
        /// </summary>
        /// <param name="handler">handler used to process commands</param>
        /// <param name="commands">commands for which to register handler</param>
        public void RegisterCommandHandler(IStreamCommandHandler handler, params string[] commands) {
            lock(commandhandlerlock)
                foreach(string command in commands)
                    commandhandlers[command] = handler;
        }

        public void UnregisterCommandHandler(IStreamCommandHandler handler) {
            lock(commandhandlerlock)
                foreach(string key in commandhandlers.Keys.ToArray())
                    if(commandhandlers[key] == handler)
                        commandhandlers.Remove(key);
        }

        void OnCommand(StreamCommand command) {
            IStreamCommandHandler handler;
            lock(commandhandlerlock)
                commandhandlers.TryGetValue(command.Command, out handler);
            if(handler == null)
                SendMessage(command.Service, command.User, $"Unknown command '{command.Command}', try '!commands' for a list of commands.", command.IsWhispered);
            else {
                try {
                    handler.ProcessStreamCommand(command);
                }
                catch(StreamCommandException e) {
                    SendMessage(command.Service, command.User, e.Message, command.IsWhispered);
                    if(e.ProvideHelp)
                        SendMessage(command.Service, command.User, $"Try '!help {command.Command}' to get an idea how to use the command.", command.IsWhispered);

                }
                catch(Exception e) {
                    Logger.Error(this, $"Error processing command '{command}'", e);
                    SendMessage(command.Service, command.User, "Error processing command. You should probably inform the streamer that this tool is crap (also he can take a look at the logs what went wrong).", command.IsWhispered);
                }
            }

            Command?.Invoke(command);
        }

        void DisplayUptime(StreamCommand command) {
            TimeSpan uptime = DateTime.Now - start;
            if(uptime.Days > 0)
                SendMessage(command.Service, command.User, $"This stream is going on for {uptime.Days} days, {uptime.Hours} hours and {uptime.Minutes} minutes now");
            else SendMessage(command.Service, command.User, $"This stream is going on for {uptime.Hours} hours and {uptime.Minutes} minutes now");
        }

        /// <summary>
        /// sends a message to the author of the specified command
        /// </summary>
        /// <param name="command">original command which led to the message</param>
        /// <param name="message">message to send</param>
        public void SendMessage(StreamCommand command, string message) {
            SendMessage(command.Service, command.User, message, command.IsWhispered);
        }

        /// <summary>
        /// sends a message to a user of a service
        /// </summary>
        /// <param name="service">service to send message to</param>
        /// <param name="user">user to send message to</param>
        /// <param name="message">message to send</param>
        /// <param name="iswhisper">whether to send a private message</param>
        public void SendMessage(string service, string user, string message, bool iswhisper = false) {
            if(string.IsNullOrEmpty(service)) {
                if(iswhisper)
                    throw new Exception("Whispered message without service is actually impossible");

                BroadcastMessage(message);
                return;
            }

            int maxlength = iswhisper ? 500 : 500 - user.Length - 2;
            string[] messages = message.SplitMessage(maxlength).ToArray();

            foreach(string chunk in messages) {
                if(iswhisper) {
                    SendPrivateMessage(service, user, chunk);
                }
                else {
                    string tosend = $"{user}: {chunk}";
                    SendMessage(service, tosend);
                }
            }
        }

        /// <summary>
        /// sends a message to all connected services
        /// </summary>
        /// <param name="message">message to send</param>
        public void BroadcastMessage(string message) {
            foreach(string service in services.Keys)
                SendMessage(service, message);
        }

        /// <summary>
        /// sends a message to a service
        /// </summary>
        /// <param name="service">service to send message to</param>
        /// <param name="message">message to send</param>
        public void SendMessage(string service, string message) {
            GetService(service).SendMessage(message);
            //if(!message.StartsWith("!"))
            //    ChatMessage?.Invoke(new ChatMessage {
            //        AvatarLink = null,
            //        Emotes = new ChatEmote[0],
            //        IsWhisper = false,
            //        Message = message,
            //        Service = null,
            //        User = "Channel",
            //        UserColor = Color.FromRgb(255,230,174)
            //    });
        }

        /// <summary>
        /// sends a private message to a user of a service
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">user to send message to</param>
        /// <param name="message">message to send</param>
        public void SendPrivateMessage(string service, string user, string message) {
            GetService(service).SendPrivateMessage(user, message);
        }

        void IRunnableModule.Stop() {
        }

        void IStreamCommandHandler.ProcessStreamCommand(StreamCommand command) {
            switch(command.Command) {
                case "commands":
                    lock(commandhandlerlock)
                        SendMessage(command.Service, command.User, $"Commandlist: {string.Join(", ", commandhandlers.Keys.Select(k => $"!{k}"))}");
                    break;
                case "uptime":
                    DisplayUptime(command);
                    break;
                case "help":
                    string helpcommand = "help";
                    if(command.Arguments.Length > 0)
                        helpcommand = command.Arguments[0];

                    IStreamCommandHandler handler;
                    lock(commandhandlerlock)
                        commandhandlers.TryGetValue(helpcommand, out handler);
                    if (handler == null)
                        SendMessage(command.Service, command.User, $"Unknown command '{helpcommand}', try !commands for a list of commands.", command.IsWhispered);
                    else {
                        string help = handler.ProvideHelp(helpcommand);
                        SendMessage(command.Service, command.User, help, command.IsWhispered);
                    }
                    break;
            }
        }

        string IStreamCommandHandler.ProvideHelp(string command) {
            switch(command) {
                case "commands":
                    return "Prints a list of supported commands. Syntax: !commands";
                case "uptime":
                    return "Prints out how long the stream is running. Syntax: !uptime";
                case "help":
                    return "Returns help on how to use a command. Syntax: !help <command>";
                default:
                    throw new ArgumentException(command);
            }
        }

        void IMessageSender.SendMessage(string message) {
            BroadcastMessage(message);
            if(message.StartsWith("!")) {
                string[] split= Commands.SplitArguments(message.Substring(1)).ToArray();
                string[] arguments = split.Skip(1).ToArray();
                foreach(KeyValuePair<string, IStreamServiceModule> streamservice in services) {
                    if(string.IsNullOrEmpty(streamservice.Value.ConnectedUser))
                        continue;

                    OnCommand(new StreamCommand {
                        Service = streamservice.Key,
                        User = streamservice.Value.ConnectedUser,
                        Command = split[0].ToLower(),
                        Arguments = arguments,
                        IsWhispered = false
                    });
                }
            }
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "uptime":
                    start = DateTime.Now - TimeSpan.Parse(arguments[0]);
                    break;
                default:
                    throw new StreamCommandException($"Command '{command}' not supported by this module.");
            }
        }
    }
}