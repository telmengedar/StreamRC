﻿using System;
using System.Collections.Generic;
using System.Net;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;

namespace StreamRC.Core.Http {

    [Module]
    public class HttpSysServiceModule : IHttpServiceModule {
        readonly HttpListener httplistener = new HttpListener();
        readonly Dictionary<string, IHttpService> servicehandlers = new Dictionary<string, IHttpService>();

        public HttpSysServiceModule() {
            httplistener.Prefixes.Add("http://localhost/streamrc/");
            httplistener.Start();
            httplistener.BeginGetContext(OnHttpContext, null);
        }

        void OnHttpContext(IAsyncResult ar) {
            httplistener.BeginGetContext(OnHttpContext, null);

            HttpListenerContext context = httplistener.EndGetContext(ar);
            string relativepath = context.Request.Url.AbsolutePath.StartsWith("/") ? context.Request.Url.AbsolutePath : context.Request.Url.AbsolutePath.GetRelativePath(httplistener.Prefixes);

            try {
                if (servicehandlers.TryGetValue(relativepath, out IHttpService service))
                {
                    context.Response.StatusCode = 200;
                    service.ProcessRequest(context.Request.ToRequest(relativepath), new HttpSysResponse(context.Response));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Service not found";
                }
            }
            catch (Exception e) {
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Fuck You";
            }

            try {
                context.Response.Close();
            }
            catch(Exception) {
                Logger.Warning(this, "You tried to close a nonexisting connection");
            }
        }

        public void AddServiceHandler(string resource, IHttpService service) {
            servicehandlers[resource] = service;
        }

        public void RemoveServiceHandler(string resource) {
            servicehandlers.Remove(resource);
        }
    }
}