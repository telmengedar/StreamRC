using System.Drawing;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters;
using StreamRC.RPG.Data;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Emotions;
using StreamRC.RPG.Equipment;
using StreamRC.RPG.Items;
using StreamRC.RPG.Players.Skills;
using StreamRC.RPG.Shops;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Messages {
    public class RPGMessageBuilder : MessageBuilder {
        readonly Context context;
        readonly GameMessageModule messagemodule;
        readonly ItemImageModule itemimages;
        readonly EmotionImageModule emotionimages;
        readonly ShopImageModule shopimages;

        public RPGMessageBuilder(Context context, GameMessageModule messagemodule, ItemImageModule itemimages, EmotionImageModule emotionimages, ShopImageModule shopimages) {
            this.context = context;
            this.messagemodule = messagemodule;
            this.itemimages = itemimages;
            this.emotionimages = emotionimages;
            this.shopimages = shopimages;
        }

        public RPGMessageBuilder ShopKeeper() {
            return Image(shopimages.GetKeeperImage()).Bold().Color(System.Drawing.Color.LightGoldenrodYellow).Text("Gangolf").Reset();
        }

        public RPGMessageBuilder Emotion(EmotionType emotion) {
            return Image(emotionimages.GetImagePath(emotion));
        }

        public RPGMessageBuilder Gold(int quantity) {
            if(quantity <= 0)
                return Image(itemimages.GetImagePath("Gold"));
            return Bold().Color(AdventureColors.Gold).Text(quantity.ToString()).Image(itemimages.GetImagePath("Gold")).Reset();
        }

        /// <summary>
        /// formats an item in a <see cref="MessageBuilder"/>
        /// </summary>
        /// <param name="item">item to represent</param>
        public RPGMessageBuilder ItemMultiple(Item item)
        {
            Bold();
            if (item.Type == ItemType.Gold)
                Color(AdventureColors.Gold);
            else
                Color(AdventureColors.Item).Text(item.GetMultiple());
            return Image(itemimages.GetImagePath(item.Name)).Reset();
        }

        /// <summary>
        /// formats an item in a <see cref="MessageBuilder"/>
        /// </summary>
        /// <param name="item">item to represent</param>
        /// <param name="quantity">quantity of item</param>
        public RPGMessageBuilder Item(Item item, int quantity = 1) {
            if(item.Type == ItemType.Gold)
                return Gold(quantity);

            Bold().Color(AdventureColors.Item);
            if(quantity > 0)
                Text(item.GetCountName(quantity));
            else Text(item.Name);
            return Image(itemimages.GetImagePath(item.Name)).Reset();
        }

        public RPGMessageBuilder EquipmentSlot(EquipmentSlot slot) {
            return Color(AdventureColors.Slot).Text(slot.ToString()).Reset();
        }

        /// <summary>
        /// formats an item in a <see cref="MessageBuilder"/>
        /// </summary>
        /// <param name="item">item to represent</param>
        public RPGMessageBuilder ItemUsage(Item item, bool critical) {
            Bold().Color(AdventureColors.Item).Text(item.GetEnumerationName(critical));
            return Image(itemimages.GetImagePath(item.Name)).Reset();
        }

        public RPGMessageBuilder BattleActor(IBattleEntity entity, bool showlevel = false) {
            if(entity == null)
                return this;

            if(entity is PlayerBattleEntity)
                return User(((PlayerBattleEntity)entity).PlayerID);
            return Monster(((MonsterBattleEntity)entity).Monster, showlevel);
        }

        public RPGMessageBuilder Monster(Monster monster, bool showlevel=false) {
            Bold().Color(AdventureColors.Monster).Text(monster.Name).Reset();
            if(showlevel)
                return Text($" Lv{monster.Level}");
            return this;
        }

        public RPGMessageBuilder User(long userid) {
            return User(context.GetModule<UserModule>().GetUser(userid));
        }

        public RPGMessageBuilder User(User user) {
            return Image(user.Avatar).Bold().Color(user.Color).Text(user.Name).Reset();
        }

        public new RPGMessageBuilder Image(string imageurl) {
            return (RPGMessageBuilder)base.Image(imageurl);
        }

        public new RPGMessageBuilder Color(string color) {
            return (RPGMessageBuilder)base.Color(color);
        }

        public new RPGMessageBuilder Color(Color color)
        {
            return (RPGMessageBuilder)base.Color(color);
        }

        public new RPGMessageBuilder Bold() {
            return (RPGMessageBuilder)base.Bold();
        }

        public new RPGMessageBuilder Reset() {
            return (RPGMessageBuilder)base.Reset();
        }

        public new RPGMessageBuilder Text(string text) {
            return (RPGMessageBuilder)base.Text(text);
        }

        public RPGMessageBuilder Skill(SkillType skill) {
            return Color(AdventureColors.Skill).Text(skill.ToString()).Reset();
        }

        public RPGMessageBuilder Damage(int damage) {
            return Color(AdventureColors.Damage).Text($"{damage} HP").Reset();
        }

        public RPGMessageBuilder Health(int hp) {
            return Color(AdventureColors.Health).Text($"{hp} HP").Reset();
        }

        public RPGMessageBuilder Experience(int xp) {
            return Color(AdventureColors.Experience).Text($"{xp} XP").Reset();
        }

        /// <summary>
        /// sends the built message
        /// </summary>
        public void Send() {
            if(HasText())
                messagemodule.SendGameMessage(BuildMessage());
        }
    }
}