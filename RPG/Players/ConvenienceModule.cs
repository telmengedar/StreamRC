using System;
using System.Linq;
using NightlyCode.Modules;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Players.Commands;
using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Players {

    [Module]
    public class ConvenienceModule {
        readonly IStreamModule stream;
        readonly UserModule users;
        readonly PlayerModule players;
        readonly SkillModule skills;
        readonly InventoryModule inventory;
        readonly ItemModule itemmodule;
        readonly EffectModule effects;

        public ConvenienceModule(IStreamModule stream, UserModule users, PlayerModule players, SkillModule skills, InventoryModule inventory, ItemModule itemmodule, EffectModule effects) {
            this.stream = stream;
            this.users = users;
            this.players = players;
            this.skills = skills;
            this.inventory = inventory;
            this.itemmodule = itemmodule;
            this.effects = effects;
            stream.RegisterCommandHandler("heal", new HealCommandHandler(this));
        }

        public void Heal(long playerid) {
            User user = users.GetUser(playerid);
            Heal(user.Service, null, user.Name);
        }

        public void Heal(string service, string channel, string username) {
            User user = users.GetExistingUser(service, username);
            Player player = players.GetExistingPlayer(service, username);
            int toheal = player.MaximumHP - player.CurrentHP;
            if(toheal == 0 && !string.IsNullOrEmpty(channel)) {
                stream.SendMessage(service, channel, username, "No need to heal yourself.");
                return;
            }

            FullInventoryItem[] items = inventory.GetInventoryItems(player.UserID, ItemType.Consumable);

            SkillConsumption skill= skills.GetSkill(player.UserID, SkillType.Heal);
            if(skill != null) {
                if(skills.GetSkillCost(SkillType.Heal, skill.Level) <= player.CurrentMP) {
                    skills.Cast(channel, user, player, SkillType.Heal);
                    return;
                }
            }

            FullInventoryItem bestitem = items.OrderBy(i => Math.Abs(toheal - i.HP)).FirstOrDefault();
            if(bestitem == null) {
                stream.SendMessage(service, channel, username, "No healing item available.");
                return;
            }

            skills.ModifyPlayerStats(player);
            effects.ModifyPlayerStats(player);

            inventory.UseItem(channel, user, player, itemmodule.GetItem(bestitem.ID));
        }
    }
}