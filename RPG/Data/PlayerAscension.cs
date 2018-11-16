using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.RPG.Data {

    /// <summary>
    /// player with the requirement for the next level
    /// </summary>
    [View("StreamRC.RPG.Data.Views.playerascension.sql")]
    public class PlayerAscension {

        /// <summary>
        /// id of user
        /// </summary>
        public long UserID { get; set; }

        /// <summary>
        /// current user level
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// current user experience
        /// </summary>
        public float Experience { get; set; }

        /// <summary>
        /// experience required for next level
        /// </summary>
        public float NextLevel { get; set; } 
    }
}