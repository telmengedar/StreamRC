using StreamRC.Core.Messages;
using StreamRC.RPG.Adventure.MonsterBattle;

namespace StreamRC.RPG.Effects {
    public class EffectResult {
        public EffectResult(EffectResultType type)
            : this(type, 0)
        {
            
        }

        public EffectResult(EffectResultType type, object argument) {
            Type = type;
            Argument = argument;
        }

        public EffectResultType Type { get; set; }

        public object Argument { get; set; }
    }
}