using System;
using NightlyCode.Core.Randoms;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Effects.Status;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Effects.Battle {
    public class ShittyWeaponEffect : IAttackEffect {
        readonly long userid;
        readonly RPGMessageModule messages;
        readonly AdventureModule adventure;

        public ShittyWeaponEffect(int level, double time, long userid, RPGMessageModule messages, AdventureModule adventure) {
            Level = level;
            Time = time;
            this.userid = userid;
            this.messages = messages;
            this.adventure = adventure;
        }

        public string Name => "ShittyWeapon";

        public void Initialize() {
        }

        public void WearOff() {
            messages.Create().Text("The weapon of ").User(userid).Text(" is not that shitty anymore.").Send();
        }

        public int Level { get; set; }

        public double Time { get; set; }

        public BattleEffectType Type => BattleEffectType.Attack;

        public EffectResult ProcessEffect(IBattleEntity attacker, IBattleEntity target) {
            if(RNG.XORShift64.NextFloat() <= Math.Min(0.25f, Level * 0.04f)) {
                return new EffectResult(EffectResultType.NewEffectTarget, new InfectionEffect(target, adventure, messages) {
                    Level = Level,
                    Time = 10.0 * Level
                });

            }
            return new EffectResult(EffectResultType.None);
        }
    }
}