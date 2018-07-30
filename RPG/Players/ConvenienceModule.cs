using System;
using System.Linq;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Players.Commands;
using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Players {

    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(PlayerModule))]
    [Dependency(nameof(InventoryModule))]
    public class ConvenienceModule : IRunnableModule {
        readonly Context context;

        public ConvenienceModule(Context context) {
            this.context = context;
        }

        public void Heal(long playerid) {
            User user = context.GetModule<UserModule>().GetUser(playerid);
            Heal(user.Service, null, user.Name);
        }

        public void Heal(string service, string channel, string username) {
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, username);
            int toheal = player.MaximumHP - player.CurrentHP;
            if(toheal == 0 && !string.IsNullOrEmpty(channel)) {
                context.GetModule<StreamModule>().SendMessage(service, channel, username, "No need to heal yourself.");
                return;
            }

            FullInventoryItem[] items = context.GetModule<InventoryModule>().GetInventoryItems(player.UserID, ItemType.Consumable);

            SkillConsumption skill=context.GetModule<SkillModule>().GetSkill(player.UserID, SkillType.Heal);
            if(skill != null) {
                if(context.GetModule<SkillModule>().GetSkillCost(SkillType.Heal, skill.Level) <= player.CurrentMP) {
                    context.GetModule<SkillModule>().Cast(channel, user, player, SkillType.Heal);
                    return;
                }
            }

            FullInventoryItem bestitem = items.OrderBy(i => Math.Abs(toheal - i.HP)).FirstOrDefault();
            if(bestitem == null) {
                context.GetModule<StreamModule>().SendMessage(service, channel, username, "No healing item available.");
                return;
            }

            context.GetModule<SkillModule>().ModifyPlayerStats(player);
            context.GetModule<EffectModule>().ModifyPlayerStats(player);

            context.GetModule<InventoryModule>().UseItem(channel, user, player, context.GetModule<ItemModule>().GetItem(bestitem.ID));
        }

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().RegisterCommandHandler("heal", new HealCommandHandler(this));
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().UnregisterCommandHandler("heal");
        }
    }
}