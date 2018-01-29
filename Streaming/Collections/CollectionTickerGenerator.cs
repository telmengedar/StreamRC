using System;
using System.Linq;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Ticker;

namespace StreamRC.Streaming.Collections {

    /// <summary>
    /// creates ticker messages for collections
    /// </summary>
    public class CollectionTickerGenerator : ITickerMessageSource {
        readonly Context context;
        readonly object countlock = new object();
        long index;

        /// <summary>
        /// creates a new <see cref="CollectionTickerGenerator"/>
        /// </summary>
        /// <param name="context">module context</param>
        /// <param name="module">access to collection module</param>
        public CollectionTickerGenerator(Context context, CollectionModule module) {
            this.context = context;
            module.CollectionAdded += collection => Recount();
            module.CollectionRemoved += collection => Recount();
            Recount();
        }

        void Recount() {
            lock(countlock)
                CollectionCount = context.Database.Load<Collection>(DBFunction.Count).ExecuteScalar<long>();
        }

        /// <summary>
        /// number of collections in database
        /// </summary>
        public long CollectionCount { get; private set; }

        public TickerMessage GenerateTickerMessage() {
            
            lock (countlock) {
                if(CollectionCount > 0) {
                    index = (index + 1) % CollectionCount;
                }
            }

            Collection collection = context.Database.LoadEntities<Collection>().Offset(index).Execute().FirstOrDefault();
            if(collection != null) {
                return new TickerMessage {
                    Content = new MessageBuilder()
                        .Text(collection.Description).Text(" Type ")
                        .Bold().Color(StreamColors.Command).Text($"!add {collection.Name} <item>").BuildMessage()
                };
            }

            throw new Exception("No collection data in database");
        }
    }
}