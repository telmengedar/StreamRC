using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using StreamRC.Core.Http;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Events {


    [Module(AutoCreate = true)]
    public class StreamEventHttpModule : IHttpService {
        readonly UserModule usermodule;
        readonly ImageCacheModule imagemodule;
        readonly StreamEventModule streameventmodule;

        public StreamEventHttpModule(IHttpServiceModule httpservice, UserModule usermodule, ImageCacheModule imagemodule, StreamEventModule streameventmodule) {
            this.usermodule = usermodule;
            this.imagemodule = imagemodule;
            this.streameventmodule = streameventmodule;
            httpservice.AddServiceHandler("/streamrc/events", this);
            httpservice.AddServiceHandler("/streamrc/events.css", this);
            httpservice.AddServiceHandler("/streamrc/events.js", this);
            httpservice.AddServiceHandler("/streamrc/events/data", this);
            httpservice.AddServiceHandler("/streamrc/highlight", this);
            httpservice.AddServiceHandler("/streamrc/highlight.css", this);
            httpservice.AddServiceHandler("/streamrc/highlight.js", this);
            httpservice.AddServiceHandler("/streamrc/highlight/data", this);
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch (request.Resource)
            {
                case "/streamrc/events":
                    response.ContentType = MimeTypes.GetMimeType(".html");
                    ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Events.events.html").CopyTo(response.Content);
                    break;
                case "/streamrc/events.css":
                    response.ContentType = MimeTypes.GetMimeType(".css");
                    ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Events.events.css").CopyTo(response.Content);
                    break;
                case "/streamrc/events.js":
                    response.ContentType = MimeTypes.GetMimeType(".js");
                    ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Events.events.js").CopyTo(response.Content);
                    break;
                case "/streamrc/events/data":
                    ServeEvents(request, response);
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
                MessageBuilder message = new MessageBuilder().User(usermodule.GetUser(streamevent.UserID), u => imagemodule.GetImageByUrl(u.Avatar));
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
                Message = new MessageBuilder().User(usermodule.GetUser(score.UserID), u => imagemodule.GetImageByUrl(u.Avatar)).Score((int)score.Score).BuildMessage()
            };
        }

        EventScore GetEvent(params StreamEventType[] types) {
            EventScore score = streameventmodule.GetLastMonthLeader(types);
            if(score == null)
                score = streameventmodule.GetLeader(types);
            return score;
        }

        void ServeEvents(IHttpRequest request, IHttpResponse response) {
            StreamHttpEvents events = new StreamHttpEvents {
                Leader = Convert("Divinity", streameventmodule.GetUserOfTheMonth()),
                Donor = Convert("Financial Pillar", GetEvent(StreamEventType.Donation, StreamEventType.Subscription)),
                Hoster = Convert("Heart of Gold", GetEvent(StreamEventType.Host, StreamEventType.Raid)),
                Support = Convert("Quality Assurance", GetEvent(StreamEventType.BugReport)),
                Social = Convert("Social Force", GetEvent(StreamEventType.Chat)),
                LastEvent = Convert(streameventmodule.GetLastEvents(1, StreamEventType.BugReport, StreamEventType.Custom, StreamEventType.Follow, StreamEventType.Host, StreamEventType.Raid).FirstOrDefault()),
                LastDonation = Convert(streameventmodule.GetLastEvents(1, StreamEventType.Donation, StreamEventType.Subscription).FirstOrDefault())
            };

            response.ContentType = MimeTypes.GetMimeType(".json");
            JSON.Write(events, response.Content);
        }
    }
}