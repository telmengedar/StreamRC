using System.Collections.Specialized;
using System.IO;

namespace StreamRC.Core.Http {

    /// <summary>
    /// interface containing data for an http request
    /// </summary>
    public interface IHttpRequest {

        /// <summary>
        /// resource which was requested
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// query string parameters
        /// </summary>
        NameValueCollection Query { get; }

        /// <summary>
        /// content type of body
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// body data
        /// </summary>
        Stream Body { get; }
    }
}