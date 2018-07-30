using System;
using System.Collections.Generic;
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

namespace StreamRC.Reviews.Http {

    [Dependency(nameof(ReviewModule))]
    [Dependency(nameof(HttpServiceModule))]
    public class ReviewHttpService : IRunnableModule, IHttpService {
        readonly Context context;
        bool available = false;

        /// <summary>
        /// creates a new <see cref="ReviewHttpService"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public ReviewHttpService(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            HttpServiceModule module = context.GetModule<HttpServiceModule>();
            
            module.AddServiceHandler("/streamrc/reviews", this);
            module.AddServiceHandler("/streamrc/reviews.css", this);
            module.AddServiceHandler("/streamrc/reviews.js", this);
            module.AddServiceHandler("/streamrc/reviews/data", this);

            context.GetModule<ReviewModule>().ReviewChanged += OnReviewChanged;
        }

        void OnReviewChanged() {
            available = true;
        }

        void IRunnableModule.Stop() {
            HttpServiceModule module = context.GetModule<HttpServiceModule>();

            module.RemoveServiceHandler("/streamrc/reviews");
            module.RemoveServiceHandler("/streamrc/reviews.css");
            module.RemoveServiceHandler("/streamrc/reviews.js");
            module.RemoveServiceHandler("/streamrc/reviews/data");

            context.GetModule<ReviewModule>().ReviewChanged -= OnReviewChanged;
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/reviews":
                    client.ServeResource(ResourceAccessor.GetResource<Stream>("StreamRC.Reviews.Http.reviews.html"), ".html");
                    break;
                case "/streamrc/reviews.css":
                    client.ServeResource(ResourceAccessor.GetResource<Stream>("StreamRC.Reviews.Http.reviews.css"), ".css");
                    break;
                case "/streamrc/reviews.js":
                    client.ServeResource(ResourceAccessor.GetResource<Stream>("StreamRC.Reviews.Http.reviews.js"), ".js");
                    break;
                case "/streamrc/reviews/data":
                    ServeReviewData(client);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        IEnumerable<HttpResultItem> CreateResults(IEnumerable<ReviewEntry> entries, double sum) {
            foreach (ReviewEntry entry in entries) {
                yield return new HttpResultItem {
                    Category = entry.Name,
                    Weight = $"{(entry.Weight * 100.0 / sum).ToString("F0")}%",
                    Value = entry.Value
                };
            }
        }

        void ServeReviewData(HttpClient client) {
            if(!available) {
                client.WriteStatus(200, "OK");
                client.WriteHeader("Content-Length", "0");
                client.EndHeader();
                return;
            }

            ReviewModule module = context.GetModule<ReviewModule>();
            using (MemoryStream ms = new MemoryStream()) {
                ReviewHttpResponse response = new ReviewHttpResponse();

                double sum = module.Entries.Sum(e => e.Weight);
                response.TimeoutEnabled = module.TimeoutEnabled;
                response.Items = CreateResults(module.Entries, sum).ToArray();
                response.Result = (int)Math.Round(module.Entries.Sum(e => e.Value * e.Weight / sum));

                JSON.Write(response, ms);
                client.ServeData(ms.ToArray(), ".json");
            }
            available = false;
        }
    }
}