using System.Drawing;
using System.Linq;
using StreamRC.Core.Messages;

namespace StreamRC.RPG.Effects {
    public class EffectMessage {
        public string MessageCode { get; set; }
        public object[] Arguments { get; set; }

        public Message CreateMessage(string player, Color playercolor, string actoravatar) {
            if(Arguments==null)
                return Message.Parse(MessageCode, player, playercolor);
            return Message.Parse(MessageCode, Arguments.Concat(new object[] {player, playercolor, actoravatar}).ToArray());
        }
    }
}