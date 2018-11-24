using System;
using NightlyCode.Core.Randoms;
using NightlyCode.Math;
using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Players.Skills.Monster {
    public class StealSkill : MonsterSkill {
        readonly PlayerModule players;
        readonly RPGMessageModule messages;

        public StealSkill(int level, PlayerModule players, RPGMessageModule messages) {
            this.players = players;
            this.messages = messages;
            Level = level;
        }


        public override string Name => "Steal";

        public override int Level { get; }

        float GetCenter()
        {
            switch (Level)
            {
                case 1:
                    return 0.5f;
                case 2:
                    return 0.66f;
                case 3:
                    return 0.72f;
                case 4:
                    return 0.77f;
                case 5:
                    return 0.8f;
                case 6:
                    return 0.82f;
                default:
                    return 0.0f;
            }
        }

        public override void Process(IBattleEntity attacker, IBattleEntity target) {
            float hitprobability = MathCore.Sigmoid(attacker.Dexterity - target.Dexterity, 1.1f, GetCenter());
            if(RNG.XORShift64.NextFloat() < hitprobability) {
                long playerid = (target as PlayerBattleEntity)?.PlayerID??0;
                int gold = Level * 50 + RNG.XORShift64.NextInt(attacker.Level * 15);
                gold = Math.Min(gold, players.GetPlayerGold(playerid));

                if(gold == 0)
                    messages.Create().BattleActor(attacker).Text(" looks at ").BattleActor(target).Text(" unable to grasp how someone can enter a battle without any ").Gold(0).Text(".").Send();
                else {
                    players.UpdateGold(playerid, -gold);
                    messages.Create().BattleActor(attacker).Text(" steals ").Gold(gold).Text(" from ").BattleActor(target).Text(", throwing it into the next hole while laughing maniacally.").Send();
                }
            }
            else messages.Create().BattleActor(attacker).Text(" fails to steal ").Gold(0).Text(" from ").BattleActor(target).Text(".").Send();
        }
    }
}