using System.Collections.Generic;
using System.Linq;
using StreamRC.Streaming.Polls;

namespace StreamRC.Streaming.Collections {
    public class CollectionDiagramData : IDiagramData {
        readonly Collection collection;
        readonly object itemlock = new object();
        readonly List<WeightedCollectionItem> items = new List<WeightedCollectionItem>();

        public CollectionDiagramData(Collection collection) {
            this.collection = collection;
        }

        public CollectionDiagramData(Collection collection, IEnumerable<WeightedCollectionItem> items)
        {
            this.collection = collection;
            this.items.AddRange(items);
        }

        public Collection Collection => collection;

        public void AddItem(WeightedCollectionItem item) {
            lock(itemlock)
                items.Add(item);
        }

        public void RemoveItem(string user, string item) {
            lock(itemlock)
                items.RemoveAll(i => i.User == user && i.Item == item);
        }

        public void RemoveItems(string item) {
            lock(itemlock)
                items.RemoveAll(i=>i.Item==item);
        }

        public IEnumerable<DiagramItem> GetItems(int count = 5) {
            Dictionary<string, int> itemcount = new Dictionary<string, int>();
            lock (itemlock)
                foreach (IGrouping<string, WeightedCollectionItem> valuegroup in items.GroupBy(v => v.Item))
                    itemcount[valuegroup.Key] = valuegroup.Sum(v => v.Status);

            int max = -1;
            foreach (KeyValuePair<string, int> result in itemcount.OrderByDescending(v => v.Value).Take(count))
            {
                if (max == -1)
                    max = result.Value;

                yield return new DiagramItem
                {
                    Item = result.Key,
                    Count = result.Value,
                    Percentage = (float)result.Value / max
                };
            }
        }
    }
}