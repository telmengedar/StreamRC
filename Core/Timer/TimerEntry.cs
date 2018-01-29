namespace StreamRC.Core.Timer {

    /// <summary>
    /// entry in timing table
    /// </summary>
    public class TimerEntry {

        /// <summary>
        /// creates a new <see cref="TimerEntry"/>
        /// </summary>
        /// <param name="service">service to be executed</param>
        /// <param name="period">interval for service execution</param>
        public TimerEntry(ITimerService service, double period) {
            Service = service;
            Period = period;
        }

        /// <summary>
        /// service executed regularly
        /// </summary>
        public ITimerService Service { get; }

        /// <summary>
        /// interval for service execution
        /// </summary>
        public double Period { get; set; }

        /// <summary>
        /// currently elapsed time of entry
        /// </summary>
        public double Time { get; set; } 
    }
}