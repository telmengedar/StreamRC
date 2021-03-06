﻿using System;
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
    [Dependency(nameof(StreamEventModule))]
    public class StreamEventHttpModule : IRunnableModule, IHttpService {
        readonly Context context;
        UserModule usermodule;
        ImageCacheModule imagemodule;
        StreamEventModule streameventmodule;

        public StreamEventHttpModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            usermodule = context.GetModule<UserModule>();
            imagemodule = context.GetModule<ImageCacheModule>();
            streameventmodule = context.GetModule<StreamEventModule>();
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
                case "/streamrc/events/data":
                    ServeEvents(client, request);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
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
            if(streamevent == null) {
                return new StreamHttpEvent {
                    Timestamp = DateTime.Now,
                    Title = new MessageBuilder().Title("Last Event").BuildMessage(),
                    Message = new MessageBuilder().None().BuildMessage()
                };
            }

            StreamHttpEvent httpevent = new StreamHttpEvent {
                Timestamp = streamevent.Timestamp
            };

            if(streamevent.Type == StreamEventType.Custom || !string.IsNullOrEmpty(streamevent.Message))
                httpevent.Message = JSON.Read<Message>(streamevent.Message);
            else {
                MessageBuilder message = new MessageBuilder().User(usermodule.GetUser(streamevent.UserID), u => imagemodule.AddImage(u.Avatar));
                int score = (int)(streamevent.Value * streamevent.Multiplicator);
                if(score > 0)
                    message.Score(score);
                httpevent.Message = message.BuildMessage();
            }

            if(!string.IsNullOrEmpty(streamevent.Title)) {
                httpevent.Title = JSON.Read<Message>(streamevent.Message);
            }
            else {
                switch(streamevent.Type) {
                    default:
                        if(!string.IsNullOrEmpty(streamevent.Title))
                            httpevent.Title = JSON.Read<Message>(streamevent.Title);
                        else httpevent.Title = new MessageBuilder().EventTitle("<unknown>").BuildMessage();
                        break;
                    case StreamEventType.Follow:
                        httpevent.Title = new MessageBuilder().EventTitle("New Follower").BuildMessage();
                        break;
                    case StreamEventType.Subscription:
                        httpevent.Title = new MessageBuilder().EventTitle("New Subscription").BuildMessage();
                        break;
                    case StreamEventType.Host:
                        httpevent.Title = new MessageBuilder().EventTitle("New Host").BuildMessage();
                        break;
                    case StreamEventType.Donation:
                        httpevent.Title = CreateDonationMessage(streamevent);
                        break;
                    case StreamEventType.BugReport:
                        httpevent.Title = new MessageBuilder().EventTitle("New Bugreport").BuildMessage();
                        break;
                    case StreamEventType.Chat:
                        httpevent.Title = new MessageBuilder().EventTitle("New Message").BuildMessage();
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

            return new MessageBuilder().Text(value, Color.FromArgb(255, 246, 97), FontWeight.Bold).Text(currency, Color.FromArgb(255, 246, 97)).EventTitle(" Donated").BuildMessage();
        }

        StreamHttpEvent Convert(string title, EventScore score) {
            if(score == null) {
                return new StreamHttpEvent {
                    Timestamp = DateTime.Now,
                    Title = new MessageBuilder().Text(title, Color.White, FontWeight.Bold).BuildMessage(),
                    Message = new MessageBuilder().Text("<none>", Color.LightGray).BuildMessage()
                };
            }

            return new StreamHttpEvent {
                Timestamp = DateTime.Now,
                Title = new MessageBuilder().Text(title, Color.White, FontWeight.Bold).BuildMessage(),
                Message = new MessageBuilder().User(usermodule.GetUser(score.UserID), u => imagemodule.AddImage(u.Avatar)).Score((int)score.Score).BuildMessage()
            };
        }

        EventScore GetEvent(params StreamEventType[] types) {
            EventScore score = streameventmodule.GetLastMonthLeader(types);
            if(score == null)
                score = streameventmodule.GetLeader(types);
            return score;
        }

        void ServeEvents(HttpClient client, HttpRequest request) {
            int count = 5;
            if(request.HasParameter("count"))
                count = request.GetParameter<int>("count");

            using (MemoryStream ms = new MemoryStream()) {
                StreamHttpEvents events = new StreamHttpEvents {
                    Leader = Convert("Divinity", streameventmodule.GetUserOfTheMonth()),
                    Donor = Convert("Financial Pillar", GetEvent(StreamEventType.Donation, StreamEventType.Subscription)),
                    Hoster = Convert("Heart of Gold", GetEvent(StreamEventType.Host, StreamEventType.Raid)),
                    Support = Convert("Quality Assurance", GetEvent(StreamEventType.BugReport)),
                    Social = Convert("Social Force", GetEvent(StreamEventType.Chat)),
                    LastEvent = Convert(streameventmodule.GetLastEvents(1, StreamEventType.BugReport, StreamEventType.Custom, StreamEventType.Follow, StreamEventType.Host, StreamEventType.Raid).FirstOrDefault()),
                    LastDonation = Convert(streameventmodule.GetLastEvents(1, StreamEventType.Donation, StreamEventType.Subscription).FirstOrDefault())
                };

                JSON.Write(events, ms);
                client.ServeData(ms.ToArray(), ".json");
            }

        }
    }
}