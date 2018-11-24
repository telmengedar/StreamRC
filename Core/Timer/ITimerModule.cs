namespace StreamRC.Core.Timer {

    /// <summary>
    /// interface for the timer management module
    /// </summary>
    public interface ITimerModule {

        /// <summary>
        /// adds a service to the timer
        /// </summary>
        /// <remarks>
        /// if interval is not specified the service is executed as often as possible (every frame, step)
        /// </remarks>
        /// <param name="service">service to be executed regularly</param>
        /// <param name="interval">interval in which to execute service (optional)</param>
        void AddService(ITimerService service, double interval=0.0);

        /// <summary>
        /// changes the interval for a registered service
        /// </summary>
        /// <param name="service">service for which to change interval</param>
        /// <param name="interval">interval at which service is to be triggered</param>
        void ChangeInterval(ITimerService service, double interval = 0.0);

        /// <summary>
        /// removes a service from the timer
        /// </summary>
        /// <param name="service">service not to be executed regularly anymore</param>
        void RemoveService(ITimerService service);
    }
}