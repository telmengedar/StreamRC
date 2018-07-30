using System;
using System.Net;

namespace StreamRC.Twitch.Chat {
    public class HeadClient : WebClient {
        protected override WebRequest GetWebRequest(Uri address) {
            WebRequest webrequest = base.GetWebRequest(address);
            webrequest.Method = "HEAD";
            return webrequest;
        }
    }
}