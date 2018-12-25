using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Data;
using NightlyCode.Core.Randoms;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters.Definitions;
using StreamRC.RPG.Items;

namespace StreamRC.RPG.Adventure.MonsterBattle.Monsters {

    [Module]
    public class MonsterModule {
        readonly ItemModule itemmodule;
        readonly Monster[] monsters;

        /// <summary>
        /// creates a new <see cref="MonsterModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public MonsterModule(ItemModule itemmodule) {
            this.itemmodule = itemmodule;
            monsters = CreateMonsters(LoadDefinitions()).ToArray();
        }

        IEnumerable<MonsterDefinition> LoadDefinitions() {
            foreach(string resource in ResourceAccessor.ListResources(typeof(MonsterModule).Assembly, "StreamRC.RPG.Adventure.MonsterBattle.Monsters.Definitions").Where(r=>r.EndsWith("json")))
                yield return JSON.Read<MonsterDefinition>(ResourceAccessor.GetResource<Stream>(resource));
        }

        IEnumerable<DropItem> CreateDrops(IEnumerable<MonsterDrop> drops) {
            if(drops == null)
                yield break;

            foreach(MonsterDrop drop in drops) {
                long itemid = itemmodule.GetItemID(drop.Item);
                if(itemid > 0)
                    yield return new DropItem {
                        ItemID = itemid,
                        Rate = drop.Rate
                    };
            }
        }

        IEnumerable<Monster> CreateMonsters(IEnumerable<MonsterDefinition> definitions) {
            foreach(MonsterDefinition definition in definitions) {
                DropItem[] drops = CreateDrops(definition.DroppedItems).ToArray();
                MonsterLevel[] levels = DataTable.ReadCSV(ResourceAccessor.GetResource<Stream>(definition.LevelResource), '\t', true).Deserialize<MonsterLevel>().ToArray();
                foreach(MonsterLevel level in levels) {
                    yield return new Monster {
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
                        Gold = level.Gold,
                        DroppedItems = drops,
                        Skills = definition.SkillSet.Where(s => s.MinLevel <= level.Level && s.MaxLevel >= level.Level).Select(s => s.Skill).Concat(new[] {
                            new SkillDefinition {
                                Type = "attack",
                                Level = 1,
                                Rate = 1.0
                            }
                        }).ToArray()
                    };
                }
            }
        } 

        public Monster GetMonster(string name, int level) {
            return monsters.Where(m => m.Requirement <= level && m.Maximum >= level && m.Name == name).RandomItem(m => Math.Min(1.0, (double)level / Math.Max(1, m.Optimum)), RNG.XORShift64);
        }
        public Monster GetMonster(int level) {
            return monsters.Where(m => m.Requirement <= level && m.Maximum >= level).RandomItem(m => Math.Min(1.0, (double)level / Math.Max(1, m.Optimum)), RNG.XORShift64);
        }
    }
}