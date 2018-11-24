using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Conversion;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
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
        /// </summa>
        /// <param name="context"></param>
        public NotificationsHttpService(IHttpServiceModule httpservice, NotificationModule notifications, TimerModule timer) {
            httpservice.AddServiceHandler("/streamrc/notifications", this);
            httpservice.AddServiceHandler("/streamrc/notifications.css", this);
            httpservice.AddServiceHandler("/streamrc/notifications.js", this);
            httpservice.AddServiceHandler("/streamrc/notifications/data", this);
            notifications.Notification += OnNotification;
            timer.AddService(this);
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/notifications":
                    response.ServeResource(GetType().Assembly, "StreamRC.Streaming.Http.Notifications.notifications.html");
                    break;
                case "/streamrc/notifications.css":
                    response.ServeResource(GetType().Assembly, "StreamRC.Streaming.Http.Notifications.notifications.css");
                    break;
                case "/streamrc/notifications.js":
                    response.ServeResource(GetType().Assembly,"StreamRC.Streaming.Http.Notifications.notifications.js");
                    break;
                case "/streamrc/notifications/data":
                    ServeNotifications(request, response);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        void ServeNotifications(IHttpRequest request, IHttpResponse response) {
            DateTime messagethreshold = Converter.Convert<DateTime>(request.GetParameter<long>("timestamp"));

            lock (notificationlock) {
                NotificationHttpResponse httpresponse = new NotificationHttpResponse {
                    Notifications = notifications.Where(n => n.Timestamp >= messagethreshold).Select(n => n.Notification).ToArray(),
                    Timestamp = DateTime.Now
                };
                response.ContentType = MimeTypes.GetMimeType(".json");
                JSON.Write(httpresponse, response.Content);
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