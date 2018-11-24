using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using StreamRC.Core.Http;

namespace StreamRC.Streaming.Polls {

    [Module(AutoCreate = true)]
    public class PollHttpService : IHttpService {
        readonly PollModule polls;
        PollHttpResponse httpresponse;

        public PollHttpService(IHttpServiceModule httpmodule, PollModule polls) {
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
            if (poll.Name == httpresponse?.Name)
                return;

            PollDiagramData diagramdata = new PollDiagramData(polls.GetWeightedVotes(poll.Name));
            diagramdata.AddOptions(polls.GetOptions(poll.Name));
            httpresponse = new PollHttpResponse
            {
                Name = poll.Name,
                Description = poll.Description,
                Items = diagramdata.GetItems().ToArray()
            };
        }

        void OnPollShown(Poll poll) {
            PreparePollData(poll);
        }

        public void ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/polls":
                    response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls.html"), ".html");
                    break;
                case "/streamrc/polls.css":
                    response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls.css"), ".css");
                    break;
                case "/streamrc/polls.js":
                    response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls.js"), ".js");
                    break;
                case "/streamrc/polls-h":
                    response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls-h.html"), ".html");
                    break;
                case "/streamrc/polls-h.css":
                    response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls-h.css"), ".css");
                    break;
                case "/streamrc/polls-h.js":
                    response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Polls.polls-h.js"), ".js");
                    break;
                case "/streamrc/polls/data":
                    ServePollData(request, response);
                    break;
            }
        }

        void ServePollData(IHttpRequest request, IHttpResponse response) {
            if (httpresponse == null) {
                if (request.GetParameter<bool>("init")) {
                    Poll poll = polls.GetPolls().RandomItem(RNG.XORShift64);
                    if (poll != null)
                        PreparePollData(poll);
                }

                if (httpresponse == null)
                    return;
            }

            int count = request.GetParameter<int>("items");
            if (count == 0) count = 5;

            PollHttpResponse clientresponse = new PollHttpResponse {
                Name = httpresponse.Name,
                Description = httpresponse.Description,
                Items = httpresponse.Items.Take(count).ToArray()
            };

            response.ContentType = MimeTypes.GetMimeType(".json");
            JSON.Write(clientresponse, response.Content);

            httpresponse = null;
        }
    }
}