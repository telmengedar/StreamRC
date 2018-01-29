using System.IO;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;

namespace StreamRC.Streaming.Polls {

    [Dependency(nameof(PollModule), DependencyType.Type)]
    [Dependency(nameof(HttpServiceModule), DependencyType.Type)]
    public class PollHttpService : IRunnableModule, IHttpService {
        readonly Context context;
        PollHttpResponse response;

        public PollHttpService(Context context) {
            this.context = context;
        }

        public void Start() {
            HttpServiceModule httpmodule = context.GetModule<HttpServiceModule>();

            httpmodule.AddServiceHandler("/streamrc/polls", this);
            httpmodule.AddServiceHandler("/streamrc/polls.css", this);
            httpmodule.AddServiceHandler("/streamrc/polls.js", this);
            httpmodule.AddServiceHandler("/streamrc/polls-h", this);
            httpmodule.AddServiceHandler("/streamrc/polls-h.css", this);
            httpmodule.AddServiceHandler("/streamrc/polls-h.js", this);
            httpmodule.AddServiceHandler("/streamrc/polls/data", this);

            context.GetModule<PollModule>().PollShown += OnPollShown;
        }

        void PreparePollData(Poll poll) {
            if (poll.Name == response?.Name)
                return;

            PollDiagramData diagramdata = new PollDiagramData(context.GetModule<PollModule>().GetWeightedVotes(poll.Name));
            diagramdata.AddOptions(context.GetModule<PollModule>().GetOptions(poll.Name));
            response = new PollHttpResponse
            {
                Name = poll.Name,
                Description = poll.Description,
                Items = diagramdata.GetItems().ToArray()
            };
        }

        void OnPollShown(Poll poll) {
            PreparePollData(poll);
        }

        public void Stop() {
            HttpServiceModule httpmodule = context.GetModule<HttpServiceModule>();

            httpmodule.RemoveServiceHandler("/streamrc/polls");
            httpmodule.RemoveServiceHandler("/streamrc/polls.css");
            httpmodule.RemoveServiceHandler("/streamrc/polls.js");
            httpmodule.RemoveServiceHandler("/streamrc/polls/data");

            context.GetModule<PollModule>().PollShown -= OnPollShown;
        }

        public void ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/polls":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls.html"), ".html");
                    break;
                case "/streamrc/polls.css":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls.css"), ".css");
                    break;
                case "/streamrc/polls.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls.js"), ".js");
                    break;
                case "/streamrc/polls-h":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls-h.html"), ".html");
                    break;
                case "/streamrc/polls-h.css":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls-h.css"), ".css");
                    break;
                case "/streamrc/polls-h.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls-h.js"), ".js");
                    break;
                case "/streamrc/polls/data":
                    ServePollData(request, client);
                    break;
            }
        }

        void ServePollData(HttpRequest request, HttpClient client) {
            if(response == null) {
                if(request.GetParameter<bool>("init")) {
                    Poll poll = context.GetModule<PollModule>().GetPolls().RandomItem(RNG.XORShift64);
                    if(poll != null)
                        PreparePollData(poll);
                }

                if(response==null) {
                    client.WriteStatus(200, "OK");
                    client.WriteHeader("Content-Length", "0");
                    client.EndHeader();
                    return;
                }
            }

            int count = request.GetParameter<int>("items");
            if(count == 0) count = 5;

            using(MemoryStream ms = new MemoryStream()) {
                PollHttpResponse clientresponse = new PollHttpResponse {
                    Name = response.Name,
                    Description = response.Description,
                    Items = response.Items.Take(count).ToArray()
                };

                JSON.Write(clientresponse, ms);
                client.ServeData(ms.ToArray(), ".json");
            }
            response = null;
        }
    }
}