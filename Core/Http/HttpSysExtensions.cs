using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace StreamRC.Core.Http {

    /// <summary>
    /// extensions for http sys implementations
    /// </summary>
    public static class HttpSysExtensions {

        public static IHttpRequest ToRequest(this HttpListenerRequest request, string resource) {
            return new HttpRequest(resource, request.QueryString, request.ContentType, request.InputStream);
        }

        /// <summary>
        /// get relative path of address to a bound prefix
        /// </summary>
        /// <param name="address">address to check</param>
        /// <param name="prefixes">prefix server is listening on</param>
        public static string GetRelativePath(this string address, IEnumerable<string> prefixes)
        {
            Match addressmatch = Regex.Match(address, @"(?<protocol>[a-zA-Z]+):\/\/(?<host>[^\/:]+)(:(?<port>[0-9]+))?\/(?<relative>[^\?]*)(\?(?<querystring>.*))?");

            if (!addressmatch.Success)
                throw new Exception("Unable to analyse address");
            string relativeaddress = addressmatch.Groups["relative"].Value;

            foreach (string prefix in prefixes)
            {
                Match prefixmatch = Regex.Match(prefix, @"(?<protocol>([a-zA-Z]+)|\*|\+):\/\/(?<host>[^\/:]+)(:(?<port>[0-9]+))?\/(?<relative>.*)");
                if (!prefixmatch.Success)
                    throw new Exception("Unable to analyse prefix");


                switch (prefixmatch.Groups["host"].Value)
                {
                case "*":
                case "+":
                    string relativeprefix = prefixmatch.Groups["relative"].Value;
                    if (relativeaddress.StartsWith(relativeprefix))
                        return "/" + relativeaddress.Substring(relativeprefix.Length);
                    break;
                default:
                    if (addressmatch.Groups["host"].Value == prefixmatch.Groups["host"].Value)
                        return "/" + relativeaddress.Substring(prefixmatch.Groups["relative"].Value.Length);
                    break;
                }
            }

            throw new Exception("address does not match any of the prefixes");
        }

    }
}