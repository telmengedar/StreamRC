using System.IO;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using StreamRC.Core.Http;

namespace StreamRC.Streaming.Polls {

    [Module(AutoCreate = true)]
    public class PollHttpService : IHttpService {
        readonly PollModule polls;
        PollHttpResponse response;

        public PollHttpService(HttpServiceModule httpmodule, PollModule polls) {
            this.polls = polls;
            httpmodule.AddServiceHandler("/streamrc/polls", this);
            httpmodule.AddServiceHandler("/streamrc/polls.css", this);
            httpmodule.AddServiceHandler("/streamrc/polls.js", this);
            httpmodule.AddServiceHandler("/streamrc/polls-h", this);
            httpmodule.AddServiceHandler("/streamrc/polls-h.css", this);
            httpmodule.AddServiceHandler("/streamrc/polls-h.js", this);
            httpmodule.AddServiceHandler("/streamrc/polls/data", this);

            polls.PollShown += OnPollShown;
        }

        void PreparePollData(Poll poll) {
            if (poll.Name == response?.Name)
                return;

            PollDiagramData diagramdata = new PollDiagramData(polls.GetWeightedVotes(poll.Name));
            diagramdata.AddOptions(polls.GetOptions(poll.Name));
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
                    Poll poll = polls.GetPolls().RandomItem(RNG.XORShift64);
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