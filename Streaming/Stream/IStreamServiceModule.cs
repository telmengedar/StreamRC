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
        /// triggered when a user follows channel
        /// </summary>
        event Action<UserInformation> NewFollower;

        /// <summary>
        /// triggered when a user has subscribed to channel
        /// </summary>
        event Action<SubscriberInformation> NewSubscriber;

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
        /// icon representing service
        /// </summary>
        System.IO.Stream ServiceIcon { get; }
    }
}