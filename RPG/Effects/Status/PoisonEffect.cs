using System;
using NightlyCode.Core.Randoms;
using NightlyCode.Modules;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Data;
using StreamRC.RPG.Effects.Modifiers;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;

namespace StreamRC.RPG.Effects.Status {
    public class PoisonEffect : IStatusEffect, IModifierEffect {
        readonly IModuleContext context;
        readonly RPGMessageModule messages;
        readonly IBattleEntity target;

        double cooldown = 5.0;

        public PoisonEffect(IModuleContext context, IBattleEntity target, RPGMessageModule messages)
        {
            this.context = context;
            this.messages = messages;
            this.target = target;
        }

        double GetCooldown() {
            switch(Level) {
                case 1:
                    return 50.0;
                case 2:
                    return 42.3;
                case 3:
                    return 37.4;
                case 4:
                    return 32.1;
                case 5:
                    return 27.7;
                case 6:
                    return 21.2;
                default:
                    return 50.0;
            }
        }

        int GetDamage()
        {
            switch (Level)
            {
                case 1:
                    return 1;
                case 2:
                    return 2;
                case 3:
                    return 2;
                case 4:
                    return 3;
                case 5:
                    return 3;
                case 6:
                    return 4;
                default:
                    return 0;
            }
        }

        public string Name => "Poison";

        public void Initialize()
        {
            cooldown = GetCooldown();
            messages.Create().BattleActor(target).Text(" was ").Color(AdventureColors.Poison).Text($"Poisoned ({Level})").Reset().Text(".").Send();
        }

        public void WearOff() {
            messages.Create().BattleActor(target).Text(" doesn't feel the effects of the ").Color(AdventureColors.Poison).Text("Poison").Reset().Text(" anymore.").Send();
        }

        public int Level { get; set; }

        public void ModifyStats(Player player) {
            player.Fitness -= Level;
        }

        public void ProcessStatusEffect(double time)
        {
            cooldown -= time;
            if (cooldown <= 0)
            {
                int damage = Math.Max(1, (int)(GetDamage() * (0.5 + RNG.XORShift64.NextDouble() * 0.5)));
                RPGMessageBuilder message = messages.Create().Text("The ").Color(AdventureColors.Poison).Text("Poison").Reset().Text(" is draining the body of ").BattleActor(target).Text(" for ").Damage(damage).Text(".").Reset();
                target.Hit(damage);
                if(target.HP <= 0) {
                    target.BattleLogic.Remove(target);
                    message.Text(" ").BattleActor(target).Text(" dies to the ").Color(AdventureColors.Poison).Text("Poison").Reset().Text(".");
                    context.GetModule<AdventureModule>().ChangeStatus(target.Adventure, AdventureStatus.SpiritRealm);
                }
                message.Send();
                cooldown += GetCooldown();
            }
        }

        public double Time { get; set; }

    }
}