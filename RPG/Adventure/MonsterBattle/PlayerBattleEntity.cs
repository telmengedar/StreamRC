using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Randoms;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters;
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
        readonly EffectModule effectmodule;
        readonly PlayerModule playermodule;
        readonly SkillModule skillmodule;
        readonly EquipmentModule equipment;
        readonly ItemModule items;
        readonly InventoryModule inventorymodule;
        readonly User user;

        public PlayerBattleEntity(long playerid, Adventure adventure, MonsterBattleLogic battlelogic, UserModule usermodule, EffectModule effectmodule, PlayerModule playermodule, SkillModule skillmodule, EquipmentModule equipment, ItemModule items, InventoryModule inventorymodule) {
            this.effectmodule = effectmodule;
            this.playermodule = playermodule;
            this.skillmodule = skillmodule;
            this.equipment = equipment;
            this.items = items;
            this.inventorymodule = inventorymodule;
            PlayerID = playerid;
            Adventure = adventure;
            BattleLogic = battlelogic;
            user = usermodule.GetUser(playerid);
            Effects = new ITemporaryEffect[0];
        }

        public long PlayerID { get; set; }

        public Adventure Adventure { get; }
        public MonsterBattleLogic BattleLogic { get; }
        public string Image => user.Avatar;
        public string Name => user.Name;
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
            effectmodule.AddPlayerEffect(PlayerID, effect);
        }

        public void Refresh() {
            Player player = playermodule.GetExistingPlayer(PlayerID);
            skillmodule.ModifyPlayerStats(player);
            effectmodule.ModifyPlayerStats(player);
            EquipmentBonus bonus = equipment.GetEquipmentBonus(PlayerID);

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

            Effects = effectmodule.GetActivePlayerEffects(player.UserID).Where(e=>e is IBattleEffect).ToArray();
        }

        public void Hit(int damage) {
            if(damage == 0)
                return;

            HP -= damage;
            playermodule.UpdateHealth(PlayerID, -damage);
        }

        public int Heal(int healing) {
            int healed= Math.Min(MaxHP, HP + healing);
            HP += healed;
            playermodule.UpdateHealth(PlayerID, healed);
            return healed;
        }

        public BattleReward Reward(IBattleEntity victim) {
            MonsterBattleEntity monster=victim as MonsterBattleEntity;
            if(monster == null)
                return null;

            playermodule.AddExperience(user.Service, user.Name, monster.Monster.Experience);
            playermodule.UpdateGold(PlayerID, monster.Monster.Gold);

            Item item = null;
            DropItem dropitem = monster.Monster.DroppedItems.FirstOrDefault(i => RNG.XORShift64.NextDouble() < i.Rate);
            if(dropitem != null) {
                item = items.GetItem(dropitem.ItemID);
                if(item != null) {
                    if(inventorymodule.AddItem(PlayerID, item.ID, 1) == AddInventoryItemResult.InventoryFull)
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