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
    public class PlaqueEffect : IStatusEffect, IModifierEffect {
        readonly IModuleContext context;
        readonly RPGMessageModule messages;
        readonly IBattleEntity target;

        double cooldown = 5.0;

        public PlaqueEffect(IModuleContext context, IBattleEntity target, RPGMessageModule messages)
        {
            this.context = context;
            this.messages = messages;
            this.target = target;
        }

        double GetCooldown() {
            switch(Level) {
                case 1:
                    return 30.0;
                case 2:
                    return 26.3;
                case 3:
                    return 23.4;
                case 4:
                    return 20.1;
                case 5:
                    return 17.7;
                case 6:
                    return 15.2;
                default:
                    return 30.0;
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
                    return 3;
                case 4:
                    return 5;
                case 5:
                    return 7;
                case 6:
                    return 10;
                default:
                    return 0;
            }
        }

        public string Name => "Plaque";

        public void Initialize()
        {
            cooldown = GetCooldown();
            messages.Create().BattleActor(target).Text(" got infected with the ").Color(AdventureColors.Plaque).Text($"Plaque Lv{Level}").Reset().Send();
        }

        public void WearOff() {
            messages.Create().BattleActor(target).Text(" is miraculously healed of the ").Color(AdventureColors.Plaque).Text("Plaque").Reset().Send();
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
                RPGMessageBuilder message = messages.Create().Text("The ").Color(AdventureColors.Plaque).Text("Plaque").Reset().Text(" is draining the body of ").BattleActor(target).Text(" for ").Damage(damage).Text(".").Reset();
                target.Hit(damage);
                if(target.HP <= 0) {
                    target.BattleLogic.Remove(target);
                    message.Text(" ").BattleActor(target).Text(" dies to the ").Color(AdventureColors.Plaque).Text("Plaque").Reset().Text(".");
                    context.GetModule<AdventureModule>().ChangeStatus(target.Adventure, AdventureStatus.SpiritRealm);
                }
                message.Send();
                cooldown += GetCooldown();
            }
        }

        public double Time { get; set; }

    }
}