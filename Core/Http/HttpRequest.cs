using System.Collections.Specialized;
using System.IO;

namespace StreamRC.Core.Http {

    /// <summary>
    /// data container for <see cref="IHttpRequest"/>s
    /// </summary>
    public class HttpRequest : IHttpRequest {

        public HttpRequest(string resource, NameValueCollection query, string contentType, Stream body) {
            Resource = resource;
            Query = query;
            ContentType = contentType;
            Body = body;
        }
        public string Resource { get; }
        public NameValueCollection Query { get; }
        public string ContentType { get; }
        public Stream Body { get; }
    }
}