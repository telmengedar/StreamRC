using System;
using NightlyCode.Core.Randoms;
using NightlyCode.StreamRC.Modules;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Effects.Status;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Effects.Battle {
    public class ShittyWeaponEffect : IAttackEffect {
        readonly Context context;
        readonly long userid;

        public ShittyWeaponEffect(Context context, int level, double time, long userid) {
            this.context = context;
            Level = level;
            Time = time;
            this.userid = userid;
        }

        public string Name => "ShittyWeapon";

        public void Initialize() {
        }

        public void WearOff() {
            context.GetModule<RPGMessageModule>().Create().Text("The weapon of ").User(userid).Text(" is not that shitty anymore.").Send();
        }

        public int Level { get; set; }

        public double Time { get; set; }

        public BattleEffectType Type => BattleEffectType.Attack;

        public EffectResult ProcessEffect(IBattleEntity attacker, IBattleEntity target) {
            if(RNG.XORShift64.NextFloat() <= Math.Min(0.25f, Level * 0.04f)) {
                return new EffectResult(EffectResultType.NewEffectTarget, new InfectionEffect(target, context.GetModule<AdventureModule>(), context.GetModule<RPGMessageModule>()) {
                    Level = Level,
                    Time = 10.0 * Level
                });

            }
            return new EffectResult(EffectResultType.None);
        }
    }
}