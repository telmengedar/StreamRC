namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// script parameter with a constant value
    /// </summary>
    public class ConstantParameter : IScriptParameter {

        /// <summary>
        /// creates a new <see cref="ConstantParameter"/>
        /// </summary>
        /// <param name="parameter">parameter value</param>
        public ConstantParameter(string parameter) {
            Parameter = parameter;
        }

        /// <summary>
        /// representation of parameter in script call
        /// </summary>
        public string Parameter { get; }

        /// <inheritdoc />
        public override string ToString() {
            return Parameter;
        }
    }
}