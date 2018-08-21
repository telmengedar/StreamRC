using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;

namespace StreamRC.Gambling.Cards
{

    /// <summary>
    /// provides images for playing cards to http requests
    /// </summary>
    [Dependency(nameof(HttpServiceModule), SpecifierType.Type)]
    public class CardImageModule : IRunnableModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="CardImageModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public CardImageModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// get url used to get playing card image from server
        /// </summary>
        /// <param name="card">card of which to get image</param>
        /// <returns>url to image resource representing the playing card</returns>
        public string GetCardUrl(Card card) {
            return $"http://localhost/streamrc/gambling/images/card?code={(int)card.Code}";
        }

        /// <summary>
        /// starts the <see cref="T:NightlyCode.Modules.IModule"/>
        /// </summary>
        void IRunnableModule.Start() {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/gambling/images/card", new CardResourceHttpService());
        }

        /// <summary>
        /// stops the <see cref="T:NightlyCode.Modules.IModule"/>
        /// </summary>
        void IRunnableModule.Stop() {
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/gambling/images/card");
        }
    }
}