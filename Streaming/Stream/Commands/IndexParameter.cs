namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// script parameter pointing to stream command argument
    /// </summary>
    public class IndexParameter : IScriptParameter {
        readonly int index;

        /// <summary>
        /// creates a new <see cref="IndexParameter"/>
        /// </summary>
        /// <param name="name">name of parameter used for syntax help</param>
        /// <param name="index">index of parameter in command arguments</param>
        public IndexParameter(string name, int index) {
            Name = name;
            this.index = index;
        }

        /// <summary>
        /// name of parameter used for syntax help
        /// </summary>
        public string Name {get;}

        /// <summary>
        /// representation of parameter in script call
        /// </summary>
        public string Parameter => $"{{{index}}}";

        /// <summary>
        /// index of parameter in command arguments
        /// </summary>
        public int Index => index;

        /// <inheritdoc />
        public override string ToString() {
            return Parameter;
        }
    }
}