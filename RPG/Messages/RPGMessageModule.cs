using NightlyCode.Modules;
using StreamRC.RPG.Emotions;
using StreamRC.RPG.Items;
using StreamRC.RPG.Shops;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Messages {

    [Module]
    public class RPGMessageModule {
        readonly GameMessageModule messages;
        readonly ItemImageModule itemimages;
        readonly EmotionImageModule emotionimages;
        readonly ShopImageModule shopimages;
        readonly UserModule users;
        readonly ImageCacheModule imagecache;

        public RPGMessageModule(GameMessageModule messages, ItemImageModule itemimages, EmotionImageModule emotionimages, ShopImageModule shopimages, UserModule users, ImageCacheModule imagecache) {
            this.messages = messages;
            this.itemimages = itemimages;
            this.emotionimages = emotionimages;
            this.shopimages = shopimages;
            this.users = users;
            this.imagecache = imagecache;
        }

        public RPGMessageBuilder Create() {
            return new RPGMessageBuilder(messages, itemimages, emotionimages, shopimages, users, imagecache);
        }
    }
}