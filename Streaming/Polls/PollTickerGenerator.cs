using System;
using System.Linq;
using NightlyCode.Database.Entities.Operations.Fields;
using StreamRC.Core;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Collections;
using StreamRC.Streaming.Ticker;
using FontWeight = StreamRC.Core.Messages.FontWeight;

namespace StreamRC.Streaming.Polls
{

    /// <summary>
    /// generates ticker messages for poll options
    /// </summary>
    public class PollTickerGenerator : ITickerMessageSource {
        readonly DatabaseModule database;
        readonly object countlock = new object();
        long index = 0;

        /// <summary>
        /// creates a new <see cref="CollectionTickerGenerator"/>
        /// </summary>
        /// <param name="context">module context</param>
        /// <param name="module">access to poll module</param>
        public PollTickerGenerator(DatabaseModule database, PollModule module)
        {
            this.database = database;
            module.OptionAdded += option => Recount();
            module.OptionRemoved += option => Recount();
            module.PollCreated += poll => Recount();
            module.PollRemoved += poll => Recount();
            Recount();
        }

        void Recount()
        {
            lock (countlock)
                PollOptionCount = database.Database.Load<PollOption>(c => DBFunction.Count).Where(p=>!p.Locked).ExecuteScalar<long>();
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

            PollOption option = database.Database.LoadEntities<PollOption>().Where(o => !o.Locked).Offset(index).Execute().FirstOrDefault();
            if (option != null) {
                Poll poll = database.Database.LoadEntities<Poll>().Where(p => p.Name == option.Poll).Execute().FirstOrDefault();

                if(poll != null) {
                    return new TickerMessage {
                        Content = new MessageBuilder()
                            .Text("Type ")
                            .Text($"!vote {option.Key}", StreamColors.Command, FontWeight.Bold)
                            .Text(" in chat for ")
                            .Text(option.Description, StreamColors.Game, FontWeight.Bold)
                            .BuildMessage()
                    };
                }
            }

            throw new Exception("No poll data in database");
        }

    }
}