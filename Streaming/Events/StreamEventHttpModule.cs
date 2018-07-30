using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Events {

    [Dependency(nameof(UserModule))]
    [Dependency(nameof(ImageCacheModule))]
    [Dependency(nameof(HttpServiceModule))]
    public class StreamEventHttpModule : IRunnableModule, IHttpService {
        readonly Context context;
        UserModule usermodule;
        ImageCacheModule imagemodule;

        public StreamEventHttpModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            usermodule = context.GetModule<UserModule>();
            imagemodule = context.GetModule<ImageCacheModule>();
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/events", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/events.css", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/events.js", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/events/data", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/highlight", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/highlight.css", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/highlight.js", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/highlight/data", this);
        }

        void IRunnableModule.Stop() {
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/events");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/events.css");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/events.js");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/events/data");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/highlight");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/highlight.css");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/highlight.js");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/highlight/data");
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
                case "/streamrc/highlight":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Events.highlight.html"), ".html");
                    break;
                case "/streamrc/highlight.css":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Events.highlight.css"), ".css");
                    break;
                case "/streamrc/highlight.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Events.highlight.js"), ".js");
                    break;
                case "/streamrc/events/data":
                    ServeEvents(client, request);
                    break;
                case "/streamrc/highlight/data":
                    ServeHighlight(client, request);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        StreamHttpEvent CreateUserOfTheMonth() {
            long userid = context.GetModule<StreamEventModule>().GetUserOfTheMonth();
            if(userid >= 0) {
                User user = usermodule.GetUser(userid);

                return new StreamHttpEvent {
                    Message = new MessageBuilder().Text("User of the ").Text("Month", Color.FromArgb(255, 118, 87), FontWeight.Bold).BuildMessage(),
                    Title = new MessageBuilder().User(user, u => imagemodule.AddImage(u.Avatar)).BuildMessage()
                };
            }

            return new StreamHttpEvent {
                Message = new MessageBuilder().Text("Biggest ").Text("Supporter", Color.FromArgb(255, 118, 87), FontWeight.Bold).BuildMessage(),
                Title = new MessageBuilder().Text("None", Color.DarkGray).BuildMessage()
            };
        }

        string GetCurrency(string service, string value) {
            switch(service) {
                case "Twitch":
                    return value=="1"?" Bitch":" Bitches";
                default:
                    return " Gold";
            }
        }

        StreamHttpEvent Convert(StreamEvent streamevent) {
            StreamHttpEvent httpevent = new StreamHttpEvent {
                Timestamp = streamevent.Timestamp
            };

            if(streamevent.Type == StreamEventType.Custom || !string.IsNullOrEmpty(streamevent.Title))
                httpevent.Title = JSON.Read<Message>(streamevent.Title);
            else httpevent.Title = new MessageBuilder().User(usermodule.GetUser(streamevent.UserID), u => imagemodule.AddImage(u.Avatar)).BuildMessage();

            if(!string.IsNullOrEmpty(streamevent.Message)) {
                httpevent.Message = JSON.Read<Message>(streamevent.Message);
            }
            else {
                switch(streamevent.Type) {
                    default:
                        httpevent.Message = JSON.Read<Message>(streamevent.Message);
                        break;
                    case StreamEventType.Follow:
                        httpevent.Message = new MessageBuilder().Text("Follow").BuildMessage();
                        break;
                    case StreamEventType.Subscription:
                        httpevent.Message = new MessageBuilder().Text("Subscription").BuildMessage();
                        break;
                    case StreamEventType.Host:
                        MessageBuilder hostbuilder = new MessageBuilder().Text("Host");
                        if(streamevent.Value >= 1.0)
                            hostbuilder.Text($" ({streamevent.Value})");
                        httpevent.Message = hostbuilder.BuildMessage();
                        break;
                    case StreamEventType.Donation:
                        httpevent.Message = CreateDonationMessage(streamevent);
                        break;
                    case StreamEventType.BugReport:
                        httpevent.Message = new MessageBuilder().Text("Bugreport", Color.PaleTurquoise).Text(": ").Text(streamevent.Argument, Color.LightGoldenrodYellow).BuildMessage();
                        break;
                }
            }

            return httpevent;
        }

        string ToValueString(double value) {
            return value.ToString((value % 1 == 0) ? "N0" : "N2", CultureInfo.InvariantCulture);
        }

        Message CreateDonationMessage(StreamEvent @event) {
            User user = usermodule.GetUser(@event.UserID);

            string value;
            string currency;

            if(string.IsNullOrEmpty(@event.Argument)) {
                value = ToValueString(@event.Value);
                currency = GetCurrency(user.Service, value);
            }
            else {
                switch(@event.Argument.ToLower()) {
                    case "$":
                    case "dollar":
                        value = ToValueString(@event.Value / 100.0);
                        currency = "$";
                        break;
                    case "€":
                    case "euro":
                        value = ToValueString(@event.Value / 100.0);
                        currency = "€";
                        break;
                    default:
                        value = ToValueString(@event.Value);
                        currency = " Gold";
                        break;
                }
            }

            return new MessageBuilder().Text("Donated ").Text(value, Color.FromArgb(255, 246, 97), FontWeight.Bold).Text(currency, Color.FromArgb(255, 246, 97)).BuildMessage();
        }

        void ServeEvents(HttpClient client, HttpRequest request) {
            int count = 5;
            if(request.HasParameter("count"))
                count = request.GetParameter<int>("count");

            using (MemoryStream ms = new MemoryStream()) {
                StreamHttpEventResponse response = new StreamHttpEventResponse {
                    Events = context.GetModule<StreamEventModule>().GetLastEvents(count).Select(Convert).ToArray()
                };

                JSON.Write(response, ms);
                client.ServeData(ms.ToArray(), ".json");
            }

        }

        void ServeHighlight(HttpClient client, HttpRequest request)
        {

            using (MemoryStream ms = new MemoryStream()) {
                StreamHttpEvent response = CreateUserOfTheMonth();
                JSON.Write(response, ms);
                client.ServeData(ms.ToArray(), ".json");
            }
        }

    }
}