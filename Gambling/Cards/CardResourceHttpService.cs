using System.IO;
using NightlyCode.Core.ComponentModel;
using StreamRC.Core.Http;

namespace StreamRC.Gambling.Cards {

    /// <summary>
    /// serves images for playing cards to http clients
    /// </summary>
    public class CardResourceHttpService : IHttpService {

        /// <summary>
        /// get path to resource containing image representing card code
        /// </summary>
        /// <param name="code">code of playing card</param>
        /// <returns>resource path</returns>
        public string GetResourcePath(byte code) {
            return $"{GetType().Namespace}.Images.Card{code + 1}.png";
        }

        /// <summary>
        /// processes the http request
        /// </summary>
        /// <param name="client">client of request</param>
        /// <param name="request">request data</param>
        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            byte code = request.GetParameter<byte>("code");
            //if(code==0||code>52)
            //    client.
            response.ServeResource(ResourceAccessor.GetResource<Stream>(GetResourcePath(code)), MimeTypes.GetMimeType(".png"));
        }
    }
}