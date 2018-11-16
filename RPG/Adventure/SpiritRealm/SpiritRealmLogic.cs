using System;
using NightlyCode.Core.Randoms;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Adventure.SpiritRealm {
    public class SpiritRealmLogic : IAdventureLogic {
        readonly PlayerModule playermodule;
        readonly UserModule usermodule;
        readonly RPGMessageModule messages;

        public SpiritRealmLogic(PlayerModule playermodule, UserModule usermodule, RPGMessageModule messages) {
            this.playermodule = playermodule;
            this.usermodule = usermodule;
            this.messages = messages;
        }

        public AdventureStatus ProcessPlayer(long playerid) {
            if(RNG.XORShift64.NextFloat() < 0.05) {
                User user = usermodule.GetUser(playerid);

                playermodule.Revive(playerid);

                Player player = playermodule.GetPlayer(playerid);

                int gold = 50 * player.Level;
                int cost = Math.Min(player.Gold, gold);

                player.Gold -= cost;
                playermodule.UpdateGold(player.UserID, -cost);

                if(cost == 0) {
                    messages.Create().ShopKeeper().Text(" found ").User(user).Text(" and carried the stinking corpse to the guild where it was revived for free.").Send();
                }
                else if (cost < gold) {
                    messages.Create().ShopKeeper().Text(" found ").User(user).Text(" and carried the stinking corpse to the guild taking all of the ").Gold(0).Text("money silently.").Send();
                }
                else {
                    messages.Create().ShopKeeper().Text(" found ").User(user).Text(" and carried the stinking corpse to the guild grabbing ").Gold(cost).Text(" from the purse while at it.").Send();
                }

                return AdventureStatus.Exploration;
            }
            return AdventureStatus.SpiritRealm;
        }

        public AdventureStatus Status => AdventureStatus.SpiritRealm;
    }
}