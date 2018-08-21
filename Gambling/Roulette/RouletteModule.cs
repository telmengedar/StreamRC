using System.Collections.Generic;
using System.Drawing;
using NightlyCode.Core.Randoms;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Core.Timer;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Gambling.Roulette
{

    /// <summary>
    /// module providing a roulette game to the chat of streaming services
    /// </summary>
    [Dependency(nameof(PlayerModule), SpecifierType.Type)]
    [Dependency(nameof(TimerModule), SpecifierType.Type)]
    [Dependency(nameof(RPGMessageModule), SpecifierType.Type)]
    [Dependency(nameof(StreamModule), SpecifierType.Type)]
    public class RouletteModule : IRunnableModule, ITimerService {
        readonly Context context;
        readonly Queue<RouletteField> history=new Queue<RouletteField>();
        readonly List<RouletteBet> currentbets = new List<RouletteBet>();
        readonly List<RouletteBet> nextbets=new List<RouletteBet>();

        readonly object betlock = new object();
        readonly object historylock = new object();

        readonly int[] numbers = {
            0,
            3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36,
            2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 32, 35,
            1, 4, 7, 10, 13, 16, 19, 22, 25, 28, 21, 34
        };

        readonly RouletteColor[] colors = {
            RouletteColor.Green,
            RouletteColor.Red, RouletteColor.Black, RouletteColor.Red, RouletteColor.Red, RouletteColor.Black, RouletteColor.Red, RouletteColor.Red, RouletteColor.Black, RouletteColor.Red, RouletteColor.Red, RouletteColor.Black, RouletteColor.Red,
            RouletteColor.Black, RouletteColor.Red, RouletteColor.Black, RouletteColor.Black, RouletteColor.Red, RouletteColor.Black, RouletteColor.Black, RouletteColor.Red, RouletteColor.Black, RouletteColor.Black, RouletteColor.Red, RouletteColor.Black,
            RouletteColor.Red, RouletteColor.Black, RouletteColor.Red, RouletteColor.Black, RouletteColor.Black, RouletteColor.Red, RouletteColor.Red, RouletteColor.Black, RouletteColor.Red, RouletteColor.Black, RouletteColor.Black, RouletteColor.Red
        };

        /// <summary>
        /// creates a new <see cref="RouletteModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public RouletteModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// places a bet in the next bet queue
        /// </summary>
        public void Bet(string service, string user, int gold, BetType type, int parameter) {
            long userid = context.GetModule<UserModule>().GetUserID(service, user);
            context.GetModule<PlayerModule>().UpdateGold(userid, -gold);

            lock(betlock) {
                nextbets.Add(new RouletteBet {
                    UserID = userid,
                    Gold = gold,
                    Type = type,
                    BetParameter = parameter
                });
            }
        }

        /// <summary>
        /// history of fields which occured during game rounds
        /// </summary>
        public IEnumerable<RouletteField> History
        {
            get
            {
                lock(historylock)
                    foreach(RouletteField field in history)
                        yield return field;
            }
        }

        Color GetColor(RouletteColor color) {
            switch(color) {
                case RouletteColor.Black:
                    return Color.LightGray;
                case RouletteColor.Green:
                    return Color.LawnGreen;
                case RouletteColor.Red:
                    return Color.OrangeRed;
                default:
                    return Color.White;
            }
        }

        int GetFactor(RouletteBet bet, int number, RouletteColor color) {
            switch(bet.Type) {
                case BetType.Plein:
                    if(number == bet.BetParameter)
                        return 36;
                    break;
                case BetType.Color:
                    if((color == RouletteColor.Red && bet.BetParameter == 0) || (color == RouletteColor.Black && bet.BetParameter == 1))
                        return 2;
                    break;
                case BetType.OddEven:
                    if(number != 0 && ((number & 1) == bet.BetParameter))
                        return 2;
                    break;
                case BetType.HalfBoard:
                    if(number > 18 * bet.BetParameter && number <= 18 * (bet.BetParameter + 1))
                        return 2;
                    break;
                case BetType.Douzaines:
                    if(number != 0 && ((number - 1) / 12) == bet.BetParameter)
                        return 3;
                    break;
                case BetType.Colonnes:
                    if(number != 0 && ((number - 1) % 3) == bet.BetParameter)
                        return 3;
                    break;
                case BetType.TransversalePlein:
                    if(number != 0 && ((number - 1) / 3) == bet.BetParameter)
                        return 12;
                    break;
                case BetType.TransversaleSimple:
                    if(number != 0 && (((number - 1) / 3) == bet.BetParameter || ((number - 1) / 3) == bet.BetParameter + 1))
                        return 6;
                    break;
                case BetType.LesQuatre:
                    if(number > 0 && number < 5)
                        return 9;
                    break;
                case BetType.LesCinq:
                    if(number > 0 && number < 6)
                        return 7;
                    break;
            }
            return 0;
        }

        void ITimerService.Process(double time) {
            if(currentbets.Count > 0) {
                int index = RNG.XORShift64.NextInt(numbers.Length);

                int number = numbers[index];
                RouletteColor color = colors[index];

                lock(historylock) {
                    history.Enqueue(new RouletteField(number, color));
                    while(history.Count > 10)
                        history.Dequeue();
                }

                RPGMessageBuilder builder = context.GetModule<RPGMessageModule>().Create();
                builder.Text("The ball fell to field ").Text($"{number} {color}", GetColor(color), FontWeight.Bold).Text(". ");

                Dictionary<long, int> winnings = new Dictionary<long, int>();
                lock (betlock) {
                    foreach(RouletteBet bet in currentbets) {
                        int factor = GetFactor(bet, number, color);
                        if(factor > 0) {
                            if(!winnings.ContainsKey(bet.UserID))
                                winnings[bet.UserID] = 0;
                            winnings[bet.UserID] += bet.Gold * factor;
                        }
                    }
                    currentbets.Clear();
                }

                foreach(KeyValuePair<long, int> win in winnings) {
                    builder.User(win.Key).Text(" won ").Gold(win.Value);
                    context.GetModule<PlayerModule>().UpdateGold(win.Key, win.Value);
                }
                builder.Send();
            }
            else {
                if(nextbets.Count > 0) {
                    lock(betlock) {
                        currentbets.AddRange(nextbets);
                        nextbets.Clear();
                    }
                    context.GetModule<RPGMessageModule>().Create().Text("A new roulette round has started.").Send();
                }
            }
        }

        void IRunnableModule.Start() {
            context.GetModule<TimerModule>().AddService(this, 20.0);
            context.GetModule<StreamModule>().RegisterCommandHandler("roulette", new RouletteCommandHandler(this, context.GetModule<PlayerModule>()));
        }

        void IRunnableModule.Stop() {
            context.GetModule<TimerModule>().RemoveService(this);
            context.GetModule<StreamModule>().UnregisterCommandHandler("roulette");
        }
    }
}