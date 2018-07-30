using System;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Stream.Commands {
    public class UptimeCommandHandler : StreamCommandHandler {
        readonly DateTime start = DateTime.Now;

        /// <summary>
        /// executes a <see cref="StreamCommand"/>
        /// </summary>
        /// <param name="channel">channel from which command was received</param>
        /// <param name="command">command to execute</param>
        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            TimeSpan uptime = DateTime.Now - start;
            if (uptime.Days > 0)
                channel.SendMessage($"This stream is going on for {uptime.Days} days, {uptime.Hours} hours and {uptime.Minutes} minutes now");
            else channel.SendMessage($"This stream is going on for {uptime.Hours} hours and {uptime.Minutes} minutes now");
        }

        /// <summary>
        /// provides help for the 
        /// </summary>
        /// <param name="channel">channel from which help request was received</param>
        /// <param name="user">use which requested help</param>
        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Prints out how long the stream is running. Syntax: !uptime");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}