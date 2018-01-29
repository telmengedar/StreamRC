using StreamRC.RPG.Adventure.MonsterBattle;
using StreamRC.RPG.Messages;

namespace StreamRC.RPG.Effects.Battle {

    /// <summary>
    /// effect in battle
    /// </summary>
    public interface IBattleEffect : ITemporaryEffect {

        /// <summary>
        /// type of battle effect
        /// </summary>
        BattleEffectType Type { get; }

        /// <summary>
        /// determines whether the effect takes effect
        /// </summary>
        /// <returns></returns>
        EffectResult ProcessEffect(IBattleEntity attacker, IBattleEntity target);
    }
}