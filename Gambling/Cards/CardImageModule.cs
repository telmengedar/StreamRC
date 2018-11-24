using NightlyCode.Modules;
using StreamRC.Core.Http;

namespace StreamRC.Gambling.Cards
{

    /// <summary>
    /// provides images for playing cards to http requests
    /// </summary>
    [Module(AutoCreate = true)]
    public class CardImageModule {

        /// <summary>
        /// creates a new <see cref="CardImageModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public CardImageModule(IHttpServiceModule httpservice) {
            httpservice.AddServiceHandler("/streamrc/gambling/images/card", new CardResourceHttpService());
        }

        /// <summary>
        /// get url used to get playing card image from server
        /// </summary>
        /// <param name="card">card of which to get image</param>
        /// <returns>url to image resource representing the playing card</returns>
        public string GetCardUrl(Card card) {
            return $"http://localhost/streamrc/gambling/images/card?code={(int)card.Code}";
        }
    }
}