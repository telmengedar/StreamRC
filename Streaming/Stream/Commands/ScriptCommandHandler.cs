using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Script;
using StreamRC.Core.Scripts;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// base implementation for a stream command handler
    /// </summary>
    public sealed class ScriptCommandHandler : StreamCommandHandler {
        readonly IScriptModule scripts;

        /// <summary>
        /// creates a new <see cref="ScriptCommandHandler"/>
        /// </summary>
        /// <param name="scripts">access to scripts used to call command</param>
        /// <param name="module">module to call</param>
        /// <param name="method">name of method or property to call</param>
        /// <param name="isproperty">determines whether to call a property or method</param>
        /// <param name="parameters">parameters for method call</param>
        public ScriptCommandHandler(IScriptModule scripts, string module, string method, bool isproperty, params IScriptParameter[] parameters) {
            this.scripts = scripts;
            Module = module.ToLower();
            Method = method.ToLower();
            IsProperty = isproperty;
            Parameters = parameters;
            ParameterCount = parameters.OfType<IndexParameter>().Any() ? (parameters.OfType<IndexParameter>().Max(i => i.Index) + 1) : 0;
        }

        /// <summary>
        /// module to call
        /// </summary>
        public string Module { get; }

        /// <summary>
        /// method to call
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// determines whether script calls a property
        /// </summary>
        public bool IsProperty { get; }

        /// <summary>
        /// parameters for method call
        /// </summary>
        public IScriptParameter[] Parameters { get; }

        /// <summary>
        /// number of parameters command call has to contain
        /// </summary>
        public int ParameterCount { get; }

        IEnumerable<string> StreamParameterNames {
            get {
                for (int i = 0; i < ParameterCount; ++i) {
                    IndexParameter parameter = Parameters.OfType<IndexParameter>().FirstOrDefault(p => p.Index == i);
                    if (parameter == null)
                        yield return "<?>";
                    else yield return $"<{parameter.Name}>";
                }
            }
        }

        /// <summary>
        /// executes a <see cref="StreamCommand"/>
        /// </summary>
        /// <param name="channel">channel from which command was received</param>
        /// <param name="command">command to execute</param>
        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            if (command.Arguments.Length < ParameterCount) {
                SendMessage(channel, command.User, $"Syntax: {command.Command} {string.Join(" ", StreamParameterNames)}");
                return;
            }

            try {
                object result;
                if (IsProperty)
                    result = scripts.Execute($"{Module}.{Method}") ?? "null";
                else {
                    if (command.Arguments.Length > ParameterCount)
                        result = scripts.Execute($"{Module}.{Method}({string.Format(string.Join<IScriptParameter>(",", Parameters), command.Arguments.Take(ParameterCount).Cast<object>().ToArray())},[{string.Join(",", command.Arguments.Skip(ParameterCount).Cast<object>().ToArray())}])", new InstanceVariableHost(new StreamCommandVariables(channel, command))) ?? "Executed";
                    else
                        result = scripts.Execute($"{Module}.{Method}({string.Format(string.Join<IScriptParameter>(",", Parameters), command.Arguments.Cast<object>().ToArray())})", new InstanceVariableHost(new StreamCommandVariables(channel, command))) ?? "Executed";
                }

                if (result is IEnumerable array)
                    result = string.Join("\n", array.Cast<object>());
                SendMessage(channel, command.User, $"{result}");

            }
            catch (Exception e) {
                SendMessage(channel, command.User, $"Error: {e.Message}");
            }
        }

        /// <summary>
        /// flags channel has to provide for a command to be accepted
        /// </summary>
        public override ChannelFlags RequiredFlags => ChannelFlags.Chat;
    }
}