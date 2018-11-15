using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.Streaming.Collections {

    [View("StreamRC.Streaming.Collections.Views.weightedcollectionitem.sql")]
    public class WeightedCollectionItem {
        public string Collection { get; set; }
        public string Item { get; set; }
        public string User { get; set; }
        public int Status { get; set; } 
    }
}