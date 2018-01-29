using System.IO;
using NightlyCode.Core.Conversion;
using NightlyCode.Japi.Json;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;

namespace StreamRC.Core.Http {

    /// <summary>
    /// extensions commonly used by http services
    /// </summary>
    public static class HttpServiceExtensions {

        /// <summary>
        /// reads a json body of a post request
        /// </summary>
        /// <param name="client">client which sent the request</param>
        /// <param name="request">post request sent by client</param>
        /// <returns>json object in body</returns>
        public static JsonObject ReadJson(this HttpClient client, HttpRequest request) {
            using(MemoryStream ms = new MemoryStream(client.ReadBody(request)))
                return JSON.Writer.Read(ms, true) as JsonObject;
        }

        /// <summary>
        /// get parameter of an request
        /// </summary>
        /// <typeparam name="T">type of parameter value</typeparam>
        /// <param name="request">request containing parameter</param>
        /// <param name="name">name of parameter</param>
        /// <returns>parameter value</returns>
        public static T GetParameter<T>(this HttpRequest request, string name) {
            if(!request.HasParameter(name))
                return default(T);
            return Converter.Convert<T>(request.GetParameter(name), true);
        }
    }
}