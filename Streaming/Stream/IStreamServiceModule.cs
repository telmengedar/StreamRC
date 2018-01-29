using System;
using System.Collections.Generic;

namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// interface for a stream service module
    /// </summary>
    public interface IStreamServiceModule {

        /// <summary>
        /// triggered when service is connected
        /// </summary>
        event Action Connected;

        /// <summary>
        /// triggered when a user has joined channel
        /// </summary>
        event Action<UserInformation> UserJoined;

        /// <summary>
        /// triggered when a user has left channel
        /// </summary>
        event Action<UserInformation> UserLeft;

        /// <summary>
        /// triggered when a command was received
        /// </summary>
        event Action<StreamCommand> CommandReceived;

        /// <summary>
        /// triggered when a user follows channel
        /// </summary>
        event Action<UserInformation> NewFollower;

        /// <summary>
        /// triggered when a user has subscribed to channel
        /// </summary>
        event Action<SubscriberInformation> NewSubscriber;

        /// <summary>
        /// triggered when channel is being hosted
        /// </summary>
        event Action<HostInformation> Hosted;

        /// <summary>
        /// triggered when chat message was received
        /// </summary>
        event Action<ChatMessage> ChatMessage;

        /// <summary>
        /// triggered when a user has donated some small amount of something
        /// </summary>
        event Action<MicroPresent> MicroPresent;

        /// <summary>
        /// get subscribers of a channel
        /// </summary>
        /// <returns>list of subscribers</returns>
        IEnumerable<SubscriberInformation> GetSubscribers();

        /// <summary>
        /// get followers of the connected channels
        /// </summary>
        /// <returns>enumeration of followers</returns>
        IEnumerable<UserInformation> GetFollowers();

        /// <summary>
        /// sends a message to the service
        /// </summary>
        /// <param name="message">message to send</param>
        void SendMessage(string message);

        /// <summary>
        /// sends a private message to a user of the service
        /// </summary>
        /// <param name="user">user to send message to</param>
        /// <param name="message">message to send</param>
        void SendPrivateMessage(string user, string message);

        /// <summary>
        /// user which is connected to service
        /// </summary>
        string ConnectedUser { get; }
    }
}