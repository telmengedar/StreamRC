using System;
using NightlyCode.Modules;
using StreamRC.Core.Http;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Http {

    /// <summary>
    /// module providing common resources for http scripting
    /// </summary>
    [Module(AutoCreate = true)]
    public class StreamHttpResourceModule : IHttpService {
        readonly StreamModule stream;

        /// <summary>
        /// creates a new <see cref="StreamHttpResourceModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public StreamHttpResourceModule(IHttpServiceModule httpservice, StreamModule stream) {
            this.stream = stream;
            httpservice.AddServiceHandler("/streamrc/services/icon", this);
            httpservice.AddServiceHandler("/streamrc/scripts/messages.js", this);
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/scripts/messages.js":
                    response.ServeResource(GetType().Assembly, "StreamRC.Streaming.Http.messages.js");
                    break;
                case "/streamrc/services/icon":
                    ServeServiceIcon(request, response);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not served by this module");
            }
        }

        void ServeServiceIcon(IHttpRequest request, IHttpResponse response) {
            string servicename = request.GetParameter<string>("service");

            IStreamServiceModule service = stream.GetService(servicename);
            response.ServeResource(service.ServiceIcon, ".png");
        }
    }
}