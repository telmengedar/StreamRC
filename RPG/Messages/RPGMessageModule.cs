using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Emotions;
using StreamRC.RPG.Items;
using StreamRC.RPG.Shops;

namespace StreamRC.RPG.Messages {

    [Dependency(nameof(ItemImageModule))]
    [Dependency(nameof(GameMessageModule))]
    public class RPGMessageModule : IRunnableModule {
        readonly Context context;
        GameMessageModule messages;
        ItemImageModule itemimages;
        EmotionImageModule emotionimages;
        ShopImageModule shopimages;

        public RPGMessageModule(Context context) {
            this.context = context;
        }

        public RPGMessageBuilder Create() {
            return new RPGMessageBuilder(context, messages, itemimages, emotionimages, shopimages);
        }

        public void Start() {
            messages = context.GetModule<GameMessageModule>();
            itemimages = context.GetModule<ItemImageModule>();
            emotionimages = context.GetModule<EmotionImageModule>();
            shopimages = context.GetModule<ShopImageModule>();
        }

        public void Stop() {
        }
    }
}