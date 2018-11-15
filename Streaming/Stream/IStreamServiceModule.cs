using System;

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
        /// icon representing service
        /// </summary>
        System.IO.Stream ServiceIcon { get; }
    }
}