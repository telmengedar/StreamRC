using System.IO;

namespace StreamRC.Core.Http {

    /// <summary>
    /// interface for a response to a http request
    /// </summary>
    public interface IHttpResponse {

        /// <summary>
        /// stream to use to write content
        /// </summary>
        Stream Content { get; }

        /// <summary>
        /// status code for response
        /// </summary>
        int Status { get; set; }

        /// <summary>
        /// content type of response
        /// </summary>
        string ContentType { get; set; }
    }
}