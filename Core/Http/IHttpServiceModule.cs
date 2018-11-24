namespace StreamRC.Core.Http {

    /// <summary>
    /// interface for a http service module
    /// </summary>
    public interface IHttpServiceModule {

        /// <summary>
        /// adds a servicehandler
        /// </summary>
        /// <param name="resource">resource for which servicehandler processes requests</param>
        /// <param name="service">service processing requests</param>
        void AddServiceHandler(string resource, IHttpService service);

        /// <summary>
        /// removes a servicehandler
        /// </summary>
        /// <param name="resource">path to http resource</param>
        void RemoveServiceHandler(string resource);
    }
}