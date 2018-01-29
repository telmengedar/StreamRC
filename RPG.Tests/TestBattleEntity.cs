using System.Collections.Generic;
using System.Drawing;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Effects;

namespace RPG.Tests {
    public class TestBattleEntity : IBattleEntity {
        int hits = 0;
        int maxdamage = 0;

        public TestBattleEntity(int level, int hp, int mp, int power, int defense, int dexterity, int luck, double scale) {
            Level = level;
            MaxHP=HP = hp;
            MP = mp;
            Power = power;
            Defense = defense;
            Dexterity = dexterity;
            Luck = luck;
            Scale = scale;
        }

        public void Reset() {
            maxdamage = 0;
        }

        public int MaxDamage => maxdamage;

        public int Hits => hits;

        public double Scale { get; set; }

        public double Median { get; set; }
        public Adventure Adventure { get; }
        public MonsterBattleLogic BattleLogic { get; }
        public string Image { get; }
        public string Name { get; }
        public Color Color { get; }
        public int Level { get; }
        public int HP { get; set; }
        public int MaxHP { get; }
        public int MP { get; set; }
        public int Power { get; set; }
        public int Defense { get; set; }
        public int Dexterity { get; set; }
        public int Luck { get; set; }
        public int WeaponOptimum { get; }
        public int ArmorOptimum { get; }

        public void AddEffect(ITemporaryEffect effect) {
        }

        public void Refresh() {
        }

        public void SendMessage() {
        }

        public void Hit(int damage) {
            if(damage > maxdamage)
                maxdamage = damage;

            Median += damage * Scale;
            ++hits;
        }

        public int Heal(int healing) {
            throw new System.NotImplementedException();
        }

        public BattleReward Reward(IBattleEntity victim) {
            return null;
        }

        public void CleanUp() {
        }

        public IEnumerable<ITemporaryEffect> Effects
        {
            get
            {
                yield break;
            }
        }

        public void AddMessage(string message) {
        }
    }
}