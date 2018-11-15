using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Randoms;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters;
using StreamRC.RPG.Adventure.MonsterBattle.Monsters.Definitions;
using StreamRC.RPG.Data;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Players.Skills;
using StreamRC.RPG.Players.Skills.Monster;

namespace StreamRC.RPG.Adventure.MonsterBattle {
    public class MonsterBattleEntity : IBattleEntity {
        readonly List<ITemporaryEffect> effects = new List<ITemporaryEffect>();

        public MonsterBattleEntity(Monster monster, Adventure adventure, MonsterBattleLogic battlelogic) {
            Monster = monster;
            Adventure = adventure;
            BattleLogic = battlelogic;
            MaxHP=HP = monster.HP;
            MP = monster.MP;
        }

        public MonsterSkill DetermineSkill() {
            MonsterSkillDefinition skill= Monster.Skills?.RandomItem(s => s.Rate, RNG.XORShift64);
            if(skill == null || skill.Type == "attack")
                return null;

            return context.GetModule<SkillModule>().GetMonsterSkill(skill.Type, skill.Level);
        }

        public void AddEffect(ITemporaryEffect effect) {
            ITemporaryEffect oldeffects = effects.FirstOrDefault(e => e.GetType() == effect.GetType());
            if(oldeffects != null)
                context.GetModule<EffectModule>().RemoveEffect(oldeffects);

            context.GetModule<EffectModule>().AddMonsterEffect(effect);
            effects.Add(effect);
        }

        public Monster Monster { get; }
        public Adventure Adventure { get; }
        public MonsterBattleLogic BattleLogic { get; }
        public string Image { get; }
        public string Name => Monster.Name;
        public Color Color => AdventureColors.Monster;
        public int Level => Monster.Level;
        public int HP { get; private set; }
        public int MaxHP { get; }
        public int MP { get; private set; }
        public int Power => Monster.Power;
        public int Defense => Monster.Defense;
        public int Dexterity => Monster.Dexterity;
        public int Luck => 0;
        public int WeaponOptimum => -1;
        public int ArmorOptimum => -1;

        public void Refresh() {
            for(int i=effects.Count-1;i>=0;--i)
                if(effects[i].Time <= 0.0)
                    effects.RemoveAt(i);
        }

        public void Hit(int damage) {
            HP -= damage;
        }

        public int Heal(int healing) {
            int healed = Math.Min(MaxHP, HP + healing);
            HP += healed;
            return healed;
        }

        public BattleReward Reward(IBattleEntity victim) {
            return null;
        }

        public void CleanUp() {
            foreach(ITemporaryEffect effect in effects)
                context.GetModule<EffectModule>().RemoveEffect(effect);
        }

        public IEnumerable<ITemporaryEffect> Effects => effects;
    }
}