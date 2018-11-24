using System.IO;
using System.Net;

namespace StreamRC.Core.Http {

    /// <summary>
    /// response data container for <see cref="IHttpServiceModule"/>
    /// </summary>
    public class HttpSysResponse : IHttpResponse {
        readonly HttpListenerResponse response;

        public HttpSysResponse(HttpListenerResponse response) {
            this.response = response;
        }

        public Stream Content => response.OutputStream;

        public int Status {
            get => response.StatusCode;
            set => response.StatusCode = value;
        }

        public string ContentType {
            get => response.ContentType;
            set => response.ContentType = value;
        }
    }
}