using StreamRC.RPG.Adventure.MonsterBattle.Monsters.Definitions;

namespace StreamRC.RPG.Adventure.MonsterBattle.Monsters {
    public class Monster {
        public string Name { get; set; }
        public int Level { get; set; }
        public int Requirement { get; set; }
        public int Optimum { get; set; }
        public int Maximum { get; set; }
        public int HP { get; set; }
        public int MP { get; set; }
        public int Power { get; set; }
        public int Defense { get; set; }
        public int Dexterity { get; set; }
        public int Experience { get; set; }
        public int Gold { get; set; }

        public DropItem[] DroppedItems { get; set; }
        public MonsterSkillDefinition[] Skills { get; set; }

        public override string ToString() {
            return $"{Name} Level {Level}";
        }
    }
}