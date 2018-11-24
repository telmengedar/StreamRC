using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Data;
using NightlyCode.Japi.Json;
using NUnit.Framework;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters.Definitions;
using StreamRC.RPG.Items;
using StreamRC.RPG.Players;

namespace RPG.Tests
{

    [TestFixture]
    public class MonsterTests {
        Item[] Items;
        LevelEntry[] Levels;

        IEnumerable<Monster> Monsters
        {
            get
            {
                foreach(string resource in ResourceAccessor.ListResources(typeof(MonsterDefinition).Assembly, "StreamRC.RPG.Adventure.MonsterBattle.Monsters.Definitions").Where(r=>r.EndsWith(".json"))) {
                    MonsterDefinition definition = JSON.Read<MonsterDefinition>(ResourceAccessor.GetResource<Stream>(typeof(MonsterDefinition).Assembly, resource));
                    MonsterLevel[] levels = DataTable.ReadCSV(ResourceAccessor.GetResource<Stream>(typeof(MonsterDefinition).Assembly, definition.LevelResource), '\t', true).Deserialize<MonsterLevel>().ToArray();
                    foreach (MonsterLevel level in levels)
                    {
                        yield return new Monster
                        {
                            Name = definition.Name,
                            Level = level.Level,
                            Requirement = level.Requirement,
                            Optimum = level.Optimum,
                            Maximum = level.Maximum,
                            HP = level.HP,
                            MP = level.MP,
                            Power = level.Power,
                            Dexterity = level.Dexterity,
                            Defense = level.Defense,
                            Experience = level.Experience,
                            Gold = level.Gold
                        };
                    }
                }
            }
        }

        [SetUp]
        public void Setup() {
            Items = DataTable.ReadCSV(ResourceAccessor.GetResource<Stream>(typeof(Item).Assembly, typeof(Item).Namespace + ".items.csv"), '\t', true).Deserialize<Item>().ToArray();
            Levels = DataTable.ReadCSV(ResourceAccessor.GetResource<Stream>(typeof(LevelEntry).Assembly, $"{typeof(LevelEntry).Namespace}.leveltable.csv"), '\t', true).Deserialize<LevelEntry>().ToArray();
        }

        TestBattleEntity CreatePlayerEntity(int level) {
            LevelEntry entry = Levels.FirstOrDefault(e => e.Level == level);
            TestBattleEntity battleentity = new TestBattleEntity(entry.Level, entry.Health, entry.Mana, entry.Strength, entry.Fitness, entry.Dexterity, 0, 1.0 / 500.0);

            int bonuspower = 0;
            int bonusdefense = 0;
            foreach(ItemEquipmentTarget target in (ItemEquipmentTarget[])Enum.GetValues(typeof(ItemEquipmentTarget))) {
                Item equipment = Items.Where(i => i.Target == target && i.LevelRequirement <= level).OrderByDescending(i => i.LevelRequirement).FirstOrDefault();
                if(equipment != null) {
                    bonuspower += equipment.Damage;
                    bonusdefense += equipment.Armor;
                }
            }
            battleentity.Power += (int)(bonuspower * 0.8f);
            battleentity.Defense += (int)(bonusdefense * 0.85f);
            return battleentity;
        }

        [Test]
        public void TestMonsterStrength([ValueSource(nameof(Monsters))] Monster monster) {
            TestBattleEntity playerentity = CreatePlayerEntity(monster.Requirement);
            TestBattleEntity monsterentity = new TestBattleEntity(monster.Level, monster.HP, monster.MP, monster.Power, monster.Defense, monster.Dexterity, 0, 1.0 / 500.0);
            MonsterBattleLogic battlelogic = new MonsterBattleLogic(null);
            battlelogic.Add(playerentity);
            battlelogic.Add(monsterentity);
            for(int i = 0; i < 1000; ++i)
                battlelogic.ProcessPlayer(0);

            float percent = playerentity.MaxDamage / (float)playerentity.HP;
            Assert.GreaterOrEqual(playerentity.Hits / 500.0, 0.35, $"{monster} hits way too rarely. Increase dexterity to fix that.");
            Assert.GreaterOrEqual(monsterentity.Hits / 500.0, 0.35, $"{monster} gets hit way too rarely. Decrease dexterity to fix that.");
            Assert.GreaterOrEqual(playerentity.Median, 0.05f * playerentity.HP, $"{monster} median damage is too low.");
            Assert.LessOrEqual(playerentity.Median, 0.3 * playerentity.HP, $"{monster} median damage is too high.");
            Assert.LessOrEqual(percent, 0.6f, $"{monster.Name} is too strong.");
            Assert.GreaterOrEqual(monsterentity.MaxDamage, 1+monster.Level/3, $"{monster} has too high defense.");
            Assert.LessOrEqual(monsterentity.MaxDamage/(float)monsterentity.HP, 0.4f, $"Defense of {monster} is too low.");

            Console.WriteLine($"Battle results vs. {monster}");
            Console.WriteLine($"Monster Hitrate: {playerentity.Hits / 500.0}");
            Console.WriteLine($"Player Hitrate: {monsterentity.Hits / 500.0}");
            Console.WriteLine($"Monster Median Damage: {playerentity.Median}");
            Console.WriteLine($"Player Median Damage: {monsterentity.Median}");
            Console.WriteLine($"Monster Maximum Damage: {playerentity.MaxDamage}");
            Console.WriteLine($"Player Maximum Damage: {monsterentity.MaxDamage}");

            playerentity = CreatePlayerEntity(Math.Min(Levels.Length, monster.Maximum));
            battlelogic = new MonsterBattleLogic(null);
            battlelogic.Add(playerentity);
            battlelogic.Add(monsterentity);
            for (int i = 0; i < 1000; ++i)
                battlelogic.ProcessPlayer(0);

            Assert.Greater(playerentity.MaxDamage, 0, $"{monster} should not appear at playerlevel {monster.Maximum} (no damage).");
        }
    }
}
