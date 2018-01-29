using System;
using System.Linq;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Collections;
using StreamRC.Streaming.Ticker;
using FontWeight = StreamRC.Core.Messages.FontWeight;

namespace StreamRC.Streaming.Polls
{
    public class PollTickerGenerator : ITickerMessageSource {
        readonly Context context;
        readonly object countlock = new object();
        long index = 0;

        /// <summary>
        /// creates a new <see cref="CollectionTickerGenerator"/>
        /// </summary>
        /// <param name="context">module context</param>
        /// <param name="module">access to poll module</param>
        public PollTickerGenerator(Context context, PollModule module)
        {
            this.context = context;
            module.OptionAdded += option => Recount();
            module.OptionRemoved += option => Recount();
            Recount();
        }

        void Recount()
        {
            lock (countlock)
                PollOptionCount = context.Database.Load<PollOption>(DBFunction.Count).Where(p=>!p.Locked).ExecuteScalar<long>();
        }

        /// <summary>
        /// number of collections in database
        /// </summary>
        public long PollOptionCount { get; private set; }

        public TickerMessage GenerateTickerMessage()
        {
            lock (countlock)
            {
                if (PollOptionCount > 0)
                {
                    index = (index + 1) % PollOptionCount;
                }
            }

            PollOption option = context.Database.LoadEntities<PollOption>().Where(o => !o.Locked).Offset(index).Execute().FirstOrDefault();
            if (option != null) {
                Poll poll = context.Database.LoadEntities<Poll>().Where(p => p.Name == option.Poll).Execute().FirstOrDefault();

                if(poll != null) {
                    return new TickerMessage {
                        Content = new MessageBuilder()
                            .Text(" Type ")
                            .Text($"!vote {poll.Name} {option.Key}", StreamColors.Command, FontWeight.Bold)
                            .Text(" for ")
                            .Text(option.Description, StreamColors.Game, FontWeight.Bold)
                            .BuildMessage()
                    };
                }
            }

            throw new Exception("No poll data in database");
        }

    }
}