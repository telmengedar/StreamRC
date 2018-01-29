using StreamRC.RPG.Players;

namespace StreamRC.RPG.Adventure {

    /// <summary>
    /// interface for adventure logic
    /// </summary>
    public interface IAdventureLogic {

        /// <summary>
        /// processes adventure for a player
        /// </summary>
        /// <param name="player">player for which to process adventure</param>
        AdventureStatus ProcessPlayer(long player);

        /// <summary>
        /// status for which adventure
        /// </summary>
        AdventureStatus Status { get; }
    }
}