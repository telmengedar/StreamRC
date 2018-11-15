﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Conversion;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using StreamRC.Core.Http;
using StreamRC.Core.Timer;

namespace StreamRC.Streaming.Ticker {

    /// <summary>
    /// manages http display for <see cref="TickerMessage"/>s
    /// </summary>
    [Module(AutoCreate = true)]
    public class TickerHttpService : IHttpService, ITimerService {

        readonly object messagelock = new object();
        readonly List<TickerHttpMessage> messages = new List<TickerHttpMessage>();

        /// <summary>
        /// creates a new <see cref="TickerHttpService"/>
        /// </summary>
        /// <param name="ticker">access to ticker data</param>
        public TickerHttpService(TickerModule ticker, TimerModule timer, HttpServiceModule httpservice) {
            httpservice.AddServiceHandler("/streamrc/ticker", this);
            httpservice.AddServiceHandler("/streamrc/ticker.css", this);
            httpservice.AddServiceHandler("/streamrc/ticker.js", this);
            httpservice.AddServiceHandler("/streamrc/ticker/data", this);

            ticker.Message += OnMessage;
            timer.AddService(this);
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request)
        {
            switch (request.Resource)
            {
                case "/streamrc/ticker":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Ticker.ticker.html"), ".html");
                    break;
                case "/streamrc/ticker.css":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Ticker.ticker.css"), ".css");
                    break;
                case "/streamrc/ticker.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Ticker.ticker.js"), ".js");
                    break;
                case "/streamrc/ticker/data":
                    ServeMessages(client, request);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        void ServeMessages(HttpClient client, HttpRequest request)
        {
            DateTime messagethreshold = Converter.Convert<DateTime>(Converter.Convert<long>(request.GetParameter("timestamp")));

            lock (messagelock)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    TickerHttpResponse response = new TickerHttpResponse
                    {
                        Messages = messages.Where(n => n.Timestamp >= messagethreshold).Select(n => n.Message).ToArray(),
                        Timestamp = DateTime.Now
                    };
                    JSON.Write(response, ms);
                    client.ServeData(ms.ToArray(), ".json");
                }
            }

        }

        void OnMessage(TickerMessage message)
        {
            lock (messagelock)
            {
                messages.Add(new TickerHttpMessage
                {
                    Message = message,
                    Timestamp = DateTime.Now,
                    Decay = 60.0
                });
            }
        }

        void ITimerService.Process(double time)
        {
            lock (messagelock)
            {
                for (int i = messages.Count - 1; i >= 0; --i)
                {
                    if ((messages[i].Decay -= time) <= 0.0)
                        messages.RemoveAt(i);
                }
            }
        }

    }
}