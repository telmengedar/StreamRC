using System;
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

namespace StreamRC.Streaming.Notifications {

    /// <summary>
    /// http service used to provide notifications for web display
    /// </summary>
    [Module(AutoCreate = true)]
    public class NotificationsHttpService : IHttpService, ITimerService {

        readonly object notificationlock = new object();
        readonly List<NotificationHttpMessage> notifications=new List<NotificationHttpMessage>();

        /// <summary>
        /// creates a new <see cref="NotificationsHttpService"/>
        /// </summary>
        /// <param name="context"></param>
        public NotificationsHttpService(HttpServiceModule httpservice, NotificationModule notifications, TimerModule timer) {
            httpservice.AddServiceHandler("/streamrc/notifications", this);
            httpservice.AddServiceHandler("/streamrc/notifications.css", this);
            httpservice.AddServiceHandler("/streamrc/notifications.js", this);
            httpservice.AddServiceHandler("/streamrc/notifications/data", this);
            notifications.Notification += OnNotification;
            timer.AddService(this);
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/notifications":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Notifications.notifications.html"), ".html");
                    break;
                case "/streamrc/notifications.css":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Notifications.notifications.css"), ".css");
                    break;
                case "/streamrc/notifications.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Notifications.notifications.js"), ".js");
                    break;
                case "/streamrc/notifications/data":
                    ServeNotifications(client, request);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        void ServeNotifications(HttpClient client, HttpRequest request) {
            DateTime messagethreshold = Converter.Convert<DateTime>(Converter.Convert<long>(request.GetParameter("timestamp")));

            lock (notificationlock)
            {
                using (MemoryStream ms = new MemoryStream()) {
                    NotificationHttpResponse response = new NotificationHttpResponse {
                        Notifications = notifications.Where(n => n.Timestamp >= messagethreshold).Select(n => n.Notification).ToArray(),
                        Timestamp = DateTime.Now
                    };
                    JSON.Write(response, ms);
                    client.ServeData(ms.ToArray(), ".json");
                }
            }

        }

        void OnNotification(Notification notification)
        {
            lock(notificationlock) {
                notifications.Add(new NotificationHttpMessage {
                    Notification = notification,
                    Timestamp = DateTime.Now,
                    Decay = 60.0
                });
            }
        }

        void ITimerService.Process(double time) {
            lock(notificationlock) {
                for(int i = notifications.Count - 1; i >= 0; --i) {
                    if((notifications[i].Decay -= time) <= 0.0)
                        notifications.RemoveAt(i);
                }
            }
        }
    }
}