using System;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;

namespace StreamRC.Streaming.Http {

    /// <summary>
    /// module providing common resources for http scripting
    /// </summary>
    [Dependency(nameof(HttpServiceModule), DependencyType.Type)]
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

            httpservice.AddServiceHandler("/streamrc/scripts/messages.js", this);
        }

        void IRunnableModule.Stop() {
            HttpServiceModule httpservice = context.GetModule<HttpServiceModule>();

            httpservice.RemoveServiceHandler("/streamrc/scripts/messages.js");
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/scripts/messages.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.messages.js"));
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not served by this module");
            }
        }
    }
}