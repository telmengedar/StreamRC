using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;

namespace StreamRC.Core.Http {

    /// <summary>
    /// interface for http service
    /// </summary>
    public interface IHttpService {

        /// <summary>
        /// processes the http request
        /// </summary>
        /// <param name="client">client of request</param>
        /// <param name="request">request data</param>
        void ProcessRequest(HttpClient client, HttpRequest request);
    }
}