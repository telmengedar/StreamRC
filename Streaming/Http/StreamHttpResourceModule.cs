using System;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Http {

    /// <summary>
    /// module providing common resources for http scripting
    /// </summary>
    [Dependency(nameof(HttpServiceModule))]
    public class StreamHttpResourceModule : IRunnableModule, IHttpService {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="StreamHttpResourceModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public StreamHttpResourceModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            HttpServiceModule httpservice = context.GetModule<HttpServiceModule>();

            httpservice.AddServiceHandler("/streamrc/services/icon", this);
            httpservice.AddServiceHandler("/streamrc/scripts/messages.js", this);
        }

        void IRunnableModule.Stop() {
            HttpServiceModule httpservice = context.GetModule<HttpServiceModule>();

            httpservice.RemoveServiceHandler("/streamrc/services/icon");
            httpservice.RemoveServiceHandler("/streamrc/scripts/messages.js");
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/scripts/messages.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.messages.js"));
                    break;
                case "/streamrc/services/icon":
                    ServeServiceIcon(client, request);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not served by this module");
            }
        }

        void ServeServiceIcon(HttpClient client, HttpRequest request) {
            string servicename = request.GetParameter("service");

            IStreamServiceModule service = context.GetModule<StreamModule>().GetService(servicename);
            client.ServeResource(service.ServiceIcon, ".png");
        }
    }
}