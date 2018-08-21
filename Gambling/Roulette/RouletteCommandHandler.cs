using System;
using System.Linq;
using NightlyCode.Core.Logs;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Gambling.Roulette {

    /// <summary>
    /// command handler for roulette command
    /// </summary>
    public class RouletteCommandHandler : StreamCommandHandler {
        readonly RouletteModule module;
        PlayerModule playermodule;

        /// <summary>
        /// creates a new <see cref="RouletteModule"/>
        /// </summary>
        /// <param name="module"></param>
        public RouletteCommandHandler(RouletteModule module, PlayerModule playermodule) {
            this.module = module;
            this.playermodule = playermodule;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            if(command.Arguments.Length == 0) {
                ProvideHelp(channel, command.User);
                return;
            }

            int gold;
            try
            {
                gold = command.Arguments.Length < 2 ? 1 : int.Parse(command.Arguments[1]);
            }
            catch (Exception e)
            {
                Logger.Error(this, $"{command.Arguments[1]} is no valid bet amount", e);
                SendMessage(channel, command.User, $"{command.Arguments[1]} is no valid bet amount");
                return;
            }

            if(playermodule.GetPlayerGold(playermodule.GetExistingPlayer(command.Service, command.User).UserID) < gold) {
                SendMessage(channel, command.User, $"You don't have {gold} gold");
                return;
            }

            if (command.Arguments[0].All(c => char.IsDigit(c))) {
                int field = int.Parse(command.Arguments[0]);
                if(field < 0 || field > 36) {
                    SendMessage(channel, command.User, $"Field {field} is not part of the board.");
                    return;
                }
                
                module.Bet(command.Service, command.User, gold, BetType.Plein, field);
                return;
            }

            switch(command.Arguments[0].ToLower()) {
                case "r":
                case "red":
                case "rouge":
                    module.Bet(command.Service, command.User, gold, BetType.Color, 0);
                    SendMessage(channel, command.User, $"You bet {gold} on red for the next roulette round");
                    break;
                case "b":
                case "black":
                case "noir":
                    module.Bet(command.Service, command.User, gold, BetType.Color, 1);
                    SendMessage(channel, command.User, $"You bet {gold} on black for the next roulette round");
                    break;
                case "o":
                case "odd":
                    module.Bet(command.Service, command.User, gold, BetType.OddEven, 1);
                    SendMessage(channel, command.User, $"You bet {gold} on odds for the next roulette round");
                    break;
                case "e":
                case "even":
                    module.Bet(command.Service, command.User, gold, BetType.OddEven, 0);
                    SendMessage(channel, command.User, $"You bet {gold} on evens for the next roulette round");
                    break;
                case "dozen1":
                    module.Bet(command.Service, command.User, gold, BetType.Douzaines, 0);
                    SendMessage(channel, command.User, $"You bet {gold} on the first dozen for the next roulette round");
                    break;
                case "row1":
                    module.Bet(command.Service, command.User, gold, BetType.Colonnes, 0);
                    SendMessage(channel, command.User, $"You bet {gold} on the first row for the next roulette round");
                    break;
                case "dozen2":
                    module.Bet(command.Service, command.User, gold, BetType.Douzaines, 1);
                    SendMessage(channel, command.User, $"You bet {gold} on the second dozen for the next roulette round");
                    break;
                case "row2":
                    module.Bet(command.Service, command.User, gold, BetType.Colonnes, 1);
                    SendMessage(channel, command.User, $"You bet {gold} on the second row for the next roulette round");
                    break;
                case "dozen3":
                    module.Bet(command.Service, command.User, gold, BetType.Douzaines, 2);
                    SendMessage(channel, command.User, $"You bet {gold} on the third dozen for the next roulette round");
                    break;
                case "row3":
                    module.Bet(command.Service, command.User, gold, BetType.Colonnes, 2);
                    SendMessage(channel, command.User, $"You bet {gold} on the third row for the next roulette round");
                    break;
                case "half1":
                    module.Bet(command.Service, command.User, gold, BetType.HalfBoard, 0);
                    SendMessage(channel, command.User, $"You bet {gold} on 1-18 for the next roulette round");
                    break;
                case "half2":
                    module.Bet(command.Service, command.User, gold, BetType.HalfBoard, 1);
                    SendMessage(channel, command.User, $"You bet {gold} on 19-36 for the next roulette round");
                    break;
                case "history":
                    SendMessage(channel, command.User, $"History of roulette fields: {string.Join(",", module.History)}");
                    break;
            }
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Play a round of roulette in this channel. Just type '!roulette <field> <bet>' to place a bet. For instance '!roulette red 5' places 5 gold coins on red. '!roulette 8 10' places 10 gold on field 8. '!roulette odd' places 1 gold on odd numbers");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}