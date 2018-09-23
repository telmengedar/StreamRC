using NightlyCode.DB.Entities.Attributes;

namespace NightlyCode.StreamRC.Gangolf.Dictionary {
    public class Word {

        [PrimaryKey, AutoIncrement]
        public long ID { get; set; }

        [Index("text")]
        public string Text { get; set; }

        [Index("class")]
        public WordClass Class { get; set; }

        [Index("attributes")]
        public WordAttribute Attributes { get; set; }

        [Index("group")]
        public int Group { get; set; }

        public override string ToString() {
            return Text;
        }
    }
}