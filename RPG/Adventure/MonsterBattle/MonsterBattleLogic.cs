using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Randoms;
using NightlyCode.Math;
using StreamRC.RPG.Data;
using StreamRC.RPG.Effects;
using StreamRC.RPG.Effects.Battle;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players.Skills.Monster;

namespace StreamRC.RPG.Adventure.MonsterBattle {

    /// <summary>
    /// battle logic
    /// </summary>
    public class MonsterBattleLogic : IAdventureLogic {
        readonly RPGMessageModule messages;
        readonly object actorlock = new object();
        readonly List<IBattleEntity> actors=new List<IBattleEntity>();
        int actor;
        
        /// <summary>
        /// creates a new <see cref="MonsterBattleLogic"/>
        /// </summary>
        /// <param name="messages">module used to present messages</param>
        public MonsterBattleLogic(RPGMessageModule messages) {
            this.messages = messages;            
        }

        public void Add(IBattleEntity entity) {
            lock(actorlock) {
                actors.Add(entity);
                if(actors.Count == 2) {
                    messages?.Create().BattleActor(actors[0], true).Text(" encounters a ").BattleActor(actors[1], true).Text(".").Send();
                    actor = RNG.XORShift64.NextInt(2);
                }
            }
        }

        public void Remove(IBattleEntity entity, RPGMessageBuilder message=null) {
            lock(actorlock) {
                actors.Remove(entity);
                if(entity is MonsterBattleEntity)
                    MonsterDefeated((MonsterBattleEntity)entity, message);
            }
        }

        public IEnumerable<IBattleEntity> Actors
        {
            get
            {
                lock(actorlock)
                    foreach(IBattleEntity entity in actors)
                        yield return entity;
            }
        } 

        public void PlayerDefeated(long playerid) {
            IBattleEntity target = Actors.FirstOrDefault(a => a is PlayerBattleEntity && ((PlayerBattleEntity)a).PlayerID == playerid);
            if(target == null)
                return;

            PlayerDefeated(target, null);
        }

        void PlayerDefeated(IBattleEntity target, RPGMessageBuilder message) {
            IBattleEntity attacker = Actors.FirstOrDefault(a => a is MonsterBattleEntity);

            message?.BattleActor(target).Text(" dies miserably and ").BattleActor(attacker).Text(" is laughing.");

            lock(actors)
                actors.Remove(target);
        }

        public void MonsterDefeated(MonsterBattleEntity monster, RPGMessageBuilder message) {
            IBattleEntity attacker = Actors.FirstOrDefault(a => a is PlayerBattleEntity);

            BattleReward reward = attacker?.Reward(monster);
            if (reward != null) {
                message?.BattleActor(attacker).Text(" has killed ").BattleActor(monster).Text(" and receives ").Experience(reward.XP).Text(" and ").Gold(reward.Gold).Text(".");

                if (reward.Item != null)
                    message?.BattleActor(attacker).Text(" finds ").Item(reward.Item).Text(" in the remains.");
            }

            lock(actors)
                actors.Remove(monster);
        }

        AdventureStatus CheckStatus(IBattleEntity attacker, IBattleEntity target, RPGMessageBuilder message) {
            if (target.HP <= 0)
            {
                if (attacker is PlayerBattleEntity) {
                    MonsterDefeated(target as MonsterBattleEntity, message);
                    return AdventureStatus.Exploration;
                }
                PlayerDefeated(target, message);
                return AdventureStatus.SpiritRealm;
            }
            return AdventureStatus.MonsterBattle;
        }

        AdventureStatus ProcessEffectResult(EffectResult result, IBattleEntity attacker, IBattleEntity target, RPGMessageBuilder message) {
            if(result==null)
                return AdventureStatus.MonsterBattle;

            switch(result.Type) {
                case EffectResultType.DamageSelf:
                    attacker.Hit((int)result.Argument);
                    return CheckStatus(attacker, target, message);
                case EffectResultType.NewEffectTarget:
                    (target as MonsterBattleEntity)?.AddEffect((ITemporaryEffect)result.Argument);
                    return AdventureStatus.MonsterBattle;
                default:
                    return AdventureStatus.MonsterBattle;
            }
        }

        public AdventureStatus ProcessPlayer(long playerid) {
            IBattleEntity attacker;
            IBattleEntity target;
            lock (actors) {
                if(actors.Count < 2)
                    return AdventureStatus.Exploration;

                foreach(IBattleEntity entity in actors)
                    entity.Refresh();


                attacker = actors[actor];
                actor = (actor + 1) % actors.Count;
                target = actors[actor];
            }

            RPGMessageBuilder message = messages?.Create();

            foreach(IBattleEffect effect in attacker.Effects.Where(t => t is IBattleEffect && ((IBattleEffect)t).Type == BattleEffectType.Persistent).Cast<IBattleEffect>()) {
                EffectResult result = effect.ProcessEffect(attacker, target);
                if(result.Type == EffectResultType.CancelAttack)
                    return AdventureStatus.MonsterBattle;

                AdventureStatus status = ProcessEffectResult(result, attacker, target, message);
                if(status != AdventureStatus.MonsterBattle) {
                    message?.Send();
                    attacker.CleanUp();
                    target.CleanUp();
                    return status;
                }                
            }

            MonsterSkill skill = (attacker as MonsterBattleEntity)?.DetermineSkill();
            if(skill != null) {
                skill.Process(attacker, target);
                AdventureStatus status= CheckStatus(attacker, target, message);
                message?.Send();
                return status;
            }

            float hitprobability = MathCore.Sigmoid(attacker.Dexterity - target.Dexterity, 1.1f, 0.7f);
            float dice = RNG.XORShift64.NextFloat();
            AdventureStatus returnstatus = AdventureStatus.MonsterBattle;

            if (dice < hitprobability) {
                bool hit = true;
                foreach(IBattleEffect effect in target.Effects.Where(t => (t as IBattleEffect)?.Type == BattleEffectType.Defense).Cast<IBattleEffect>()) {
                    if(effect.ProcessEffect(attacker, target).Type == EffectResultType.CancelAttack) {
                        hit = false;
                        break;
                    }
                }

                if(hit) {
                    bool damagecritical = attacker.WeaponOptimum > 0 && RNG.XORShift64.NextFloat() < (float)attacker.Luck / attacker.WeaponOptimum;
                    bool armorcritical = target.ArmorOptimum > 0 && RNG.XORShift64.NextFloat() < (float)target.Luck / target.ArmorOptimum;

                    int power = damagecritical ? attacker.Power * 2 : attacker.Power;
                    int armor = armorcritical ? target.Defense * 2 : target.Defense;

                    int damage = (int)Math.Max(0, (power - armor) * (0.5f + 0.5f * dice / hitprobability));
                    if(damage <= 0) {
                        message?.BattleActor(target);
                        if(armorcritical)
                            message?.Bold();

                        message?.Text(" deflects ").Reset().BattleActor(attacker).Text("'s attack.");
                        target.Hit(0);
                    }
                    else {
                        message?.BattleActor(attacker);
                        if (damagecritical)
                            message?.Bold();

                        message?.Text(armorcritical ? " clashes with " : " hits ");
                        message?.Reset().BattleActor(target).Text(" for ").Damage(damage).Text(".");

                        IBattleEffect effect = attacker.Effects.FirstOrDefault(e => e is ShittyWeaponEffect) as IBattleEffect;
                        ProcessEffectResult(effect?.ProcessEffect(attacker, target), attacker, target, message);

                        target.Hit(damage);
                        returnstatus = CheckStatus(attacker, target, message);
                        if(returnstatus == AdventureStatus.MonsterBattle) {
                            if(target is PlayerBattleEntity)
                                message?.Text(" ").BattleActor(target).Text(" has ").Health(target.HP).Text(" left.");
                        }
                        else {
                            attacker.CleanUp();
                            target.CleanUp();
                        }
                    }
                }
            }
            else {
                message?.BattleActor(attacker).Text(" attacks ").BattleActor(target).Text(" but ").Color(AdventureColors.Miss).Text("misses").Reset().Text(".");
            }

            message?.Send();
            return returnstatus;
        }

        public AdventureStatus Status => AdventureStatus.MonsterBattle;
    }
}