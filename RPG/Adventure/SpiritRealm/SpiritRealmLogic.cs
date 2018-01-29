using System;
using NightlyCode.Core.Randoms;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.RPG.Data;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Adventure.SpiritRealm {
    public class SpiritRealmLogic : IAdventureLogic {
        readonly Context context;

        public SpiritRealmLogic(Context context) {
            this.context = context;
        }

        public AdventureStatus ProcessPlayer(long playerid) {
            if(RNG.XORShift64.NextFloat() < 0.05) {
                User user = context.GetModule<UserModule>().GetUser(playerid);

                context.GetModule<PlayerModule>().Revive(playerid);

                Player player = context.GetModule<PlayerModule>().GetPlayer(playerid);

                int gold = 50 * player.Level;
                int cost = Math.Min(player.Gold, gold);

                player.Gold -= cost;
                context.GetModule<PlayerModule>().UpdateGold(player.UserID, -cost);

                if(cost == 0) {
                    context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" found ").User(user).Text(" and carried the stinking corpse to the guild where it was revived for free.").Send();
                }
                else if (cost < gold) {
                    context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" found ").User(user).Text(" and carried the stinking corpse to the guild taking all of the ").Gold(0).Text("money silently.").Send();
                }
                else {
                    context.GetModule<RPGMessageModule>().Create().ShopKeeper().Text(" found ").User(user).Text(" and carried the stinking corpse to the guild grabbing ").Gold(cost).Text(" from the purse while at it.").Send();
                }

                return AdventureStatus.Exploration;
            }
            return AdventureStatus.SpiritRealm;
        }

        public AdventureStatus Status => AdventureStatus.SpiritRealm;
    }
}