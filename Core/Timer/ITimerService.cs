namespace StreamRC.Core.Timer {

    /// <summary>
    /// interface for a timer service
    /// </summary>
    public interface ITimerService {

        /// <summary>
        /// processes a method regularly
        /// </summary>
        /// <param name="time">time which passed since last execution</param>
        void Process(double time);
    }
}