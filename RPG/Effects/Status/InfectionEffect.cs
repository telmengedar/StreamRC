using System;
using NightlyCode.Core.Randoms;
using StreamRC.Core.Messages;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Data;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Effects.Status {
    public class InfectionEffect : IStatusEffect {
        readonly AdventureModule adventuremodule;
        readonly RPGMessageModule messages;
        readonly IBattleEntity target;

        double cooldown = 5.0;

        public InfectionEffect(IBattleEntity target, AdventureModule adventuremodule, RPGMessageModule messages) {
            this.adventuremodule = adventuremodule;
            this.messages = messages;
            this.target = target;
        }


        int GetDamage() {
            switch(Level) {
                case 1:
                    return 1;
                case 2:
                    return 2;
                case 3:
                    return 4;
                case 4:
                    return 6;
                case 5:
                    return 9;
                case 6:
                    return 12;
                default:
                    return 0;
            }
        }

        double GetSuppurationChance() {
            switch(Level) {
                case 1:
                    return 0.08;
                case 2:
                    return 0.12;
                case 3:
                    return 0.16;
                case 4:
                    return 0.21;
                case 5:
                    return 0.26;
                case 6:
                    return 0.32;
                default:
                    return 0.0;
            }
        }

        public string Name => "Infection";

        public void Initialize() {
            cooldown = 5.0;
            messages.Create().BattleActor(target).Text($" got a {(Level >= 5 ? "nasty diverse manifold" : "fecal")} infection.").Send();
        }

        public void WearOff() {
            messages.Create().BattleActor(target).Text("'s infection wore off.").Send();
        }

        public int Level { get; set; }

        public void ProcessStatusEffect(double time) {
            cooldown -= time;
            if(cooldown <= 0) {
                if(RNG.XORShift64.NextFloat() < GetSuppurationChance()) {
                    RPGMessageBuilder message = messages.Create();
                    int damage = Math.Max(1, (int)(GetDamage() * (0.5 + RNG.XORShift64.NextDouble() * 0.5)));
                    message.Text("The wound of ").BattleActor(target).Text(" suppurates, inflicting pain for ").Damage(damage).Text(".");
                    target.Hit(damage);
                    if(target.HP <= 0)
                        target.BattleLogic.Remove(target, message);
                    message.Send();
                }
                cooldown += 5.0;
            }
        }

        public double Time { get; set; }
    }
}