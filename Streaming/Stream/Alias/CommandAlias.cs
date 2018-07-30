using NightlyCode.DB.Entities.Attributes;

namespace StreamRC.Streaming.Stream.Alias {

    /// <summary>
    /// alias handled by <see cref="CommandAliasModule"/>
    /// </summary>
    public class CommandAlias {

        /// <summary>
        /// alias which is used to trigger target command
        /// </summary>
        [PrimaryKey]
        public string Alias { get; set; }

        /// <summary>
        /// command to be executed when command is encountered
        /// </summary>
        public string Command { get; set; } 
    }
}