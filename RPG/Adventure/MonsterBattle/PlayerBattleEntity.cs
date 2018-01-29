using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NightlyCode.Core.Randoms;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters;
using StreamRC.RPG.Data;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Effects.Battle;
using StreamRC.RPG.Equipment;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Items;
using StreamRC.RPG.Players;
using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Adventure.MonsterBattle {
    public class PlayerBattleEntity : IBattleEntity {
        readonly Context context;
        readonly User user;

        public PlayerBattleEntity(Context context, long playerid, Adventure adventure, MonsterBattleLogic battlelogic) {
            this.context = context;
            PlayerID = playerid;
            Adventure = adventure;
            BattleLogic = battlelogic;
            user = context.GetModule<UserModule>().GetUser(playerid);
            Name = context.Database.Load<User>(u => u.Name).Where(u => u.ID == playerid).ExecuteScalar<string>();
            Image= context.Database.Load<User>(u => u.Avatar).Where(u => u.ID == playerid).ExecuteScalar<string>();
            Effects = new ITemporaryEffect[0];
        }

        public long PlayerID { get; set; }

        public Adventure Adventure { get; }
        public MonsterBattleLogic BattleLogic { get; }
        public string Image { get; }
        public string Name { get; }
        public int Level { get; private set; }
        public int HP { get; private set; }
        public int MaxHP { get; private set; }
        public int MP { get; private set; }
        public int Power { get; private set; }
        public int Defense { get; private set; }
        public int Dexterity { get; private set; }
        public int Luck { get; private set; }
        public int WeaponOptimum { get; private set; }
        public int ArmorOptimum { get; private set; }

        public void AddEffect(ITemporaryEffect effect) {
            context.GetModule<EffectModule>().AddPlayerEffect(PlayerID, effect);
        }

        public void Refresh() {
            Player player = context.GetModule<PlayerModule>().GetExistingPlayer(PlayerID);
            context.GetModule<SkillModule>().ModifyPlayerStats(player);
            context.GetModule<EffectModule>().ModifyPlayerStats(player);
            EquipmentBonus bonus = context.GetModule<EquipmentModule>().GetEquipmentBonus(PlayerID);

            Level = player.Level;
            HP = player.CurrentHP;
            MP = player.CurrentMP;
            MaxHP = player.MaximumHP;
            Power = player.Strength + bonus.Damage;
            Defense = player.Fitness + bonus.Armor;
            Luck = player.Luck;
            Dexterity = player.Dexterity;
            WeaponOptimum = bonus.WeaponCritical;
            ArmorOptimum = bonus.ArmorCritical;

            Effects = context.GetModule<EffectModule>().GetActivePlayerEffects(player.UserID).Where(e=>e is IBattleEffect).ToArray();
        }

        public void Hit(int damage) {
            if(damage == 0)
                return;

            HP -= damage;
            context.GetModule<PlayerModule>().UpdateHealth(PlayerID, -damage);
        }

        public int Heal(int healing) {
            int healed= Math.Min(MaxHP, HP + healing);
            HP += healed;
            context.GetModule<PlayerModule>().UpdateHealth(PlayerID, healed);
            return healed;
        }

        public BattleReward Reward(IBattleEntity victim) {
            MonsterBattleEntity monster=victim as MonsterBattleEntity;
            if(monster == null)
                return null;

            context.GetModule<PlayerModule>().AddExperience(user.Service, user.Name, monster.Monster.Experience);
            context.GetModule<PlayerModule>().UpdateGold(PlayerID, monster.Monster.Gold);

            Item item = null;
            DropItem dropitem = monster.Monster.DroppedItems.FirstOrDefault(i => RNG.XORShift64.NextDouble() < i.Rate);
            if(dropitem != null) {
                item = context.GetModule<ItemModule>().GetItem(dropitem.ItemID);
                if(item != null) {
                    if(context.GetModule<InventoryModule>().AddItem(PlayerID, item.ID, 1) == AddInventoryItemResult.InventoryFull)
                        item = null;
                }
            }

            return new BattleReward {
                XP = monster.Monster.Experience,
                Gold = monster.Monster.Gold,
                Item = item
            };
        }

        public void CleanUp() {
        }

        public IEnumerable<ITemporaryEffect> Effects { get; private set; }
    }
}