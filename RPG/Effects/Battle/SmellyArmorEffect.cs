using System;
using NightlyCode.Core.Randoms;
using StreamRC.Core.Messages;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Data;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Effects.Battle {
    public class SmellyArmorEffect : IBattleEffect {
        readonly RPGMessageModule messages;
        readonly long userid;

        public SmellyArmorEffect(int level, double time, RPGMessageModule messages, long userid) {
            Level = level;
            Time = time;
            this.messages = messages;
            this.userid = userid;
        }

        public string Name => "SmellyArmor";

        public void Initialize() {
        }

        public void WearOff() {
            messages.Create().Text("The armor of ").User(userid).Text(" doesn't smell like pee anymore.").Send();
        }

        public int Level { get; set; }

        public double Time { get; set; }

        public BattleEffectType Type => BattleEffectType.Defense;

        public EffectResult ProcessEffect(IBattleEntity attacker, IBattleEntity target) {
            float deflectchance = Math.Min(0.15f, Level * 0.03f);
            if (RNG.XORShift64.NextFloat() <= deflectchance)
            {
                messages.Create().BattleActor(attacker).Text(" is disgusted by the urinal smell of ").BattleActor(target).Text(" and refrains from attacking.").Send();
                return new EffectResult(EffectResultType.CancelAttack);
            }
            return new EffectResult(EffectResultType.None);
        }
    }
}