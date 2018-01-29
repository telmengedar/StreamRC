using System;
using System.Linq;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Players {

    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(PlayerModule), DependencyType.Type)]
    [Dependency(nameof(InventoryModule), DependencyType.Type)]
    public class ConvenienceModule : IStreamCommandHandler, IRunnableModule {
        readonly Context context;

        public ConvenienceModule(Context context) {
            this.context = context;
        }

        void IStreamCommandHandler.ProcessStreamCommand(StreamCommand command) {
            switch(command.Command) {
                case "heal":
                    Heal(command.Service, command.User, command.IsWhispered);
                    break;
            }
        }

        public void Heal(long playerid) {
            User user = context.GetModule<UserModule>().GetUser(playerid);
            Heal(user.Service, user.Name, false);
        }

        void Heal(string service, string username, bool whispered) {
            User user = context.GetModule<UserModule>().GetExistingUser(service, username);
            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(service, username);
            int toheal = player.MaximumHP - player.CurrentHP;
            if(toheal == 0) {
                context.GetModule<StreamModule>().SendMessage(service, username, "No need to heal yourself.", whispered);
                return;
            }

            FullInventoryItem[] items = context.GetModule<InventoryModule>().GetInventoryItems(player.UserID, ItemType.Consumable);

            SkillConsumption skill=context.GetModule<SkillModule>().GetSkill(player.UserID, SkillType.Heal);
            if(skill != null) {
                if(context.GetModule<SkillModule>().GetSkillCost(SkillType.Heal, skill.Level) <= player.CurrentMP) {
                    context.GetModule<SkillModule>().Cast(user, player, SkillType.Heal);
                    return;
                }
            }

            FullInventoryItem bestitem = items.OrderBy(i => Math.Abs(toheal - i.HP)).FirstOrDefault();
            if(bestitem == null) {
                context.GetModule<StreamModule>().SendMessage(service, username, "No healing item available.", whispered);
                return;
            }

            context.GetModule<SkillModule>().ModifyPlayerStats(player);
            context.GetModule<EffectModule>().ModifyPlayerStats(player);

            context.GetModule<InventoryModule>().UseItem(user, player, context.GetModule<ItemModule>().GetItem(bestitem.ID));
        }

        string IStreamCommandHandler.ProvideHelp(string command) {
            switch(command) {
                case "heal":
                    return "Heals your character with the best method available.";
                default:
                    throw new StreamCommandException($"'{command}' not handled by this module");
            }
        }

        void IRunnableModule.Start() {
            context.GetModule<StreamModule>().RegisterCommandHandler(this, "heal");
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().UnregisterCommandHandler(this);
        }
    }
}