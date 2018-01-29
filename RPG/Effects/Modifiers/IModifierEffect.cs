using StreamRC.RPG.Players;

namespace StreamRC.RPG.Effects.Modifiers {

    /// <summary>
    /// interface for an effect which modifies player statistics
    /// </summary>
    public interface IModifierEffect : ITemporaryEffect {

        /// <summary>
        /// modifies the status of the specified player
        /// </summary>
        /// <param name="player"></param>
        void ModifyStats(Player player);
    }
}