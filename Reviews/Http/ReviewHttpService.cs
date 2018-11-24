using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using StreamRC.Core.Http;

namespace StreamRC.Reviews.Http {

    [Module]
    public class ReviewHttpService : IHttpService {
        readonly ReviewModule reviews;
        bool available;

        /// <summary>
        /// creates a new <see cref="ReviewHttpService"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public ReviewHttpService(IHttpServiceModule httpservice, ReviewModule reviews) {
            this.reviews = reviews;
            httpservice.AddServiceHandler("/streamrc/reviews", this);
            httpservice.AddServiceHandler("/streamrc/reviews.css", this);
            httpservice.AddServiceHandler("/streamrc/reviews.js", this);
            httpservice.AddServiceHandler("/streamrc/reviews/data", this);

            reviews.ReviewChanged += OnReviewChanged;
        }

        void OnReviewChanged() {
            available = true;
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/reviews":
                    response.ServeResource(ResourceAccessor.GetResource<Stream>("StreamRC.Reviews.Http.reviews.html"), ".html");
                    break;
                case "/streamrc/reviews.css":
                    response.ServeResource(ResourceAccessor.GetResource<Stream>("StreamRC.Reviews.Http.reviews.css"), ".css");
                    break;
                case "/streamrc/reviews.js":
                    response.ServeResource(ResourceAccessor.GetResource<Stream>("StreamRC.Reviews.Http.reviews.js"), ".js");
                    break;
                case "/streamrc/reviews/data":
                    ServeReviewData(request, response);
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

        void ServeReviewData(IHttpRequest request, IHttpResponse response) {
            if(!available)
                return;

            using (MemoryStream ms = new MemoryStream()) {
                ReviewHttpResponse httpresponse = new ReviewHttpResponse();

                double sum = reviews.Entries.Sum(e => e.Weight);
                httpresponse.TimeoutEnabled = reviews.TimeoutEnabled;
                httpresponse.Items = CreateResults(reviews.Entries, sum).ToArray();
                httpresponse.Result = (int)Math.Round(reviews.Entries.Sum(e => e.Value * e.Weight / sum));

                response.ServeJSON(httpresponse);
            }
            available = false;
        }
    }
}