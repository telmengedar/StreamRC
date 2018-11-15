using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Windows.Media;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using StreamRC.Core.Http;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Games;

namespace StreamRC.Streaming.Statistics
{

    /// <summary>
    /// provides an html window for chat messages
    /// </summary>
    [Module(AutoCreate = true)]
    public class StatisticHttpService : IHttpService {
        readonly GameTimeModule gametime;
        readonly StatisticModule statistics;

        /// <summary>
        /// creates a new <see cref="StatisticHttpService"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public StatisticHttpService(HttpServiceModule httpservice, GameTimeModule gametime, StatisticModule statistics) {
            this.gametime = gametime;
            this.statistics = statistics;
            httpservice.AddServiceHandler("/streamrc/statistics", this);
            httpservice.AddServiceHandler("/streamrc/statistics.css", this);
            httpservice.AddServiceHandler("/streamrc/statistics.js", this);
            httpservice.AddServiceHandler("/streamrc/statistics/data", this);
            httpservice.AddServiceHandler("/streamrc/statistics/image", this);
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/statistics":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Statistics.statistics.html"), ".html");
                    break;
                case "/streamrc/statistics.css":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Statistics.statistics.css"), ".css");
                    break;
                case "/streamrc/statistics.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Statistics.statistics.js"), ".js");
                    break;
                case "/streamrc/statistics/data":
                    ServeMessages(client, request);
                    break;
                case "/streamrc/statistics/image":
                    ServerImage(client, request);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        void ServerImage(HttpClient client, HttpRequest request) {
            client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>($"StreamRC.Streaming.Statistics.Icons.{request.GetParameter<string>("name")}.png"), ".png");
        }

        IEnumerable<MessageChunk> CreateStatisticChunks(Statistic statistic) {
            switch(statistic.Name) {
                case "Game Time":
                    TimeSpan time = TimeSpan.FromTicks(statistic.Value);
                    
                    time += gametime.GetTime();
                    yield return new MessageChunk(MessageChunkType.Emoticon, "http://localhost/streamrc/statistics/image?name=gametime");
                    yield return new MessageChunk(MessageChunkType.Text, (int)time.TotalHours + time.ToString("\\:mm"));
                    break;
                case "Deaths":
                    yield return new MessageChunk(MessageChunkType.Emoticon, "http://localhost/streamrc/statistics/image?name=deaths");
                    yield return new MessageChunk(MessageChunkType.Text, statistic.Value.ToString());
                    break;
                case "Backseats":
                    yield return new MessageChunk(MessageChunkType.Emoticon, "http://localhost/streamrc/statistics/image?name=backseats");
                    yield return new MessageChunk(MessageChunkType.Text, statistic.Value.ToString());
                    break;
                case "Triumphs":
                    yield return new MessageChunk(MessageChunkType.Emoticon, "http://localhost/streamrc/statistics/image?name=triumphs");
                    yield return new MessageChunk(MessageChunkType.Text, statistic.Value.ToString());
                    break;
                default:
                    yield return new MessageChunk(MessageChunkType.Text, statistic.Name, Colors.White, FontWeight.Bold);
                    yield return new MessageChunk(": ");
                    yield return new MessageChunk(MessageChunkType.Text, statistic.Value.ToString());
                    break;
            }
        }

        HttpStatistic CreateStatistic(Statistic statistic) {
            return new HttpStatistic {
                Name = statistic.Name,
                Content = CreateStatisticChunks(statistic).ToArray()
            };
        }

        void ServeMessages(HttpClient client, HttpRequest request) {
            using(MemoryStream ms = new MemoryStream()) {
                StatisticsHttpResponse response = new StatisticsHttpResponse {
                    Statistics = statistics.Get().Select(CreateStatistic).ToArray()
                };
                JSON.Write(response, ms);
                client.ServeData(ms.ToArray(), ".json");
            }
        }
    }
}