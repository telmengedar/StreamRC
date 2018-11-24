using System.IO;
using System.Linq;
using System.Reflection;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Conversion;
using NightlyCode.Japi.Json;

namespace StreamRC.Core.Http {

    /// <summary>
    /// extensions commonly used by http services
    /// </summary>
    public static class HttpServiceExtensions {

        /// <summary>
        /// get parameter of an request
        /// </summary>
        /// <typeparam name="T">type of parameter value</typeparam>
        /// <param name="request">request containing parameter</param>
        /// <param name="name">name of parameter</param>
        /// <returns>parameter value</returns>
        public static T GetParameter<T>(this IHttpRequest request, string name) {
            return Converter.Convert<T>(request.Query[name], true);
        }

        /// <summary>
        /// determines whether the request contains a query string value with the specified key name
        /// </summary>
        /// <param name="request">request of which to check parameters</param>
        /// <param name="name">name of key to check for</param>
        /// <returns>true if query string contains the specified key, false otherwise</returns>
        public static bool HasParameter(this IHttpRequest request, string name) {
            return request.Query.AllKeys.Any(k => k == name);
        }

        /// <summary>
        /// serves data of a stream to a http response
        /// </summary>
        /// <param name="response">response to fill</param>
        /// <param name="stream">stream containing data</param>
        /// <param name="mimetype">mime type to serve</param>
        public static void ServeResource(this IHttpResponse response, Stream stream, string mimetype) {
            response.ContentType = MimeTypes.GetMimeType(mimetype);
            stream.CopyTo(response.Content);
        }

        /// <summary>
        /// serves data of a stream to an http resource
        /// </summary>
        /// <param name="response">response to fill</param>
        /// <param name="assembly">assembly containing resource</param>
        /// <param name="resource">path to resource in <see cref="assembly"/></param>
        /// <param name="mimetype">mimetype of data (optional)</param>
        public static void ServeResource(this IHttpResponse response, Assembly assembly, string resource, string mimetype = null) {
            if (mimetype == null)
                mimetype = Path.GetExtension(resource);
            response.ServeResource(ResourceAccessor.GetResource<Stream>(assembly, resource), mimetype);
        }

        /// <summary>
        /// serves data as response body
        /// </summary>
        /// <param name="response">response to fill</param>
        /// <param name="data">data to write to response</param>
        /// <param name="mimetype">mime type to set</param>
        public static void ServeData(this IHttpResponse response, byte[] data, string mimetype) {
            response.ContentType = MimeTypes.GetMimeType(mimetype);
            response.Content.Write(data, 0, data.Length);
        }

        /// <summary>
        /// serves data in json format to a client
        /// </summary>
        /// <param name="response">response to fill</param>
        /// <param name="data">data to write as json</param>
        public static void ServeJSON(this IHttpResponse response, object data) {
            response.ContentType = MimeTypes.GetMimeType(".json");
            JSON.Write(data, response.Content);
        }
    }
}