namespace StreamRC.Core.Http {

    /// <summary>
    /// interface for http service
    /// </summary>
    public interface IHttpService {

        /// <summary>
        /// processes the http request
        /// </summary>
        /// <param name="request">request data</param>
        /// <param name="response">response object to use to provide response</param>
        void ProcessRequest(IHttpRequest request, IHttpResponse response);
    }
}