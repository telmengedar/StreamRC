using System;
using System.Collections.Generic;
using System.Net;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Core.Http {

    /// <summary>
    /// module providing http service
    /// </summary>
    public class HttpServiceModule : IRunnableModule {
        Context context;
        readonly HttpServer server = new HttpServer(IPAddress.Any, 80);

        readonly Dictionary<string, IHttpService> servicehandlers = new Dictionary<string, IHttpService>();

        /// <summary>
        /// creates a new <see cref="HttpServiceModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public HttpServiceModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// adds a servicehandler
        /// </summary>
        /// <param name="resource">resource for which servicehandler processes requests</param>
        /// <param name="service">service processing requests</param>
        public void AddServiceHandler(string resource, IHttpService service) {
            servicehandlers[resource] = service;
        }

        /// <summary>
        /// removes a servicehandler
        /// </summary>
        /// <param name="resource">path to http resource</param>
        public void RemoveServiceHandler(string resource) {
            servicehandlers.Remove(resource);
        }

        void IRunnableModule.Start() {
            server.Request += OnRequest;
            server.Start();
        }

        void IRunnableModule.Stop()
        {
            server.Request -= OnRequest;
            server.Stop();
        }

        void OnRequest(HttpClient client, HttpRequest request) {
            IHttpService service;
            if(servicehandlers.TryGetValue(request.Resource, out service)) {
                try {
                    service.ProcessRequest(client, request);
                }
                catch(Exception e) {
                    Logger.Error(this, "Error processing request", e);
                    client.WriteStatus(500, "Internal Server Error");
                    client.EndHeader();
                }
                

            }
            else {
                client.WriteStatus(404, "No service found handling the request");
                client.EndHeader();
            }
        }
    }
}