using System;
using System.IO;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;
using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Events {

    [Dependency(nameof(HttpServiceModule), DependencyType.Type)]
    public class StreamEventHttpModule : IRunnableModule, IHttpService {
        readonly Context context;

        public StreamEventHttpModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/events", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/events.css", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/events.js", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/events/data", this);
        }

        void IRunnableModule.Stop() {
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/events");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/events.css");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/events.js");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/events/data");
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch (request.Resource)
            {
                case "/streamrc/events":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Events.events.html"), ".html");
                    break;
                case "/streamrc/events.css":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Events.events.css"), ".css");
                    break;
                case "/streamrc/events.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Events.events.js"), ".js");
                    break;
                case "/streamrc/events/data":
                    ServeEvents(client, request);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        void ServeEvents(HttpClient client, HttpRequest request) {
            int count = 5;
            if(request.HasParameter("count"))
                count = request.GetParameter<int>("count");

            using (MemoryStream ms = new MemoryStream()) {
                StreamHttpEvent[] response = context.GetModule<StreamEventModule>().GetLastEvents(count).Select(e => new StreamHttpEvent() {
                    Timestamp = e.Timestamp,
                    Title = JSON.Read<Message>(e.Title),
                    Message = JSON.Read<Message>(e.Message)
                }).ToArray();

                JSON.Write(response, ms);
                client.ServeData(ms.ToArray(), ".json");
            }

        }
    }
}