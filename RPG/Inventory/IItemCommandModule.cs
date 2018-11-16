using NightlyCode.Modules;
using StreamRC.RPG.Players;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Inventory {

    /// <summary>
    /// interface for a module which is able to execute item commands
    /// </summary>
    public interface IItemCommandModule {

        /// <summary>
        /// executes an item command
        /// </summary>
        /// <param name="user">user which executes the command</param>
        /// <param name="player">player data for user</param>
        /// <param name="command">command to be executed</param>
        /// <param name="arguments">arguments for command</param>
        void ExecuteItemCommand(User user, Player player, string command, params string[] arguments);
    }
}