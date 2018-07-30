using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
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
using StreamRC.Streaming.Games;

namespace StreamRC.Streaming.Statistics
{

    /// <summary>
    /// provides an html window for chat messages
    /// </summary>
    [Dependency(nameof(HttpServiceModule))]
    [Dependency(nameof(StatisticModule), SpecifierType.Type)]
    [Dependency(nameof(GameTimeModule), SpecifierType.Type)]
    public class StatisticHttpService : IInitializableModule, IHttpService {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="ChatHttpService"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public StatisticHttpService(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/statistics", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/statistics.css", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/statistics.js", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/statistics/data", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/statistics/image", this);
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
                    
                    time += context.GetModule<GameTimeModule>().GetTime();
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
                    Statistics = context.GetModule<StatisticModule>().Get().Select(CreateStatistic).ToArray()
                };
                JSON.Write(response, ms);
                client.ServeData(ms.ToArray(), ".json");
            }
        }
    }
}