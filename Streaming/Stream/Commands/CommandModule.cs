using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Core.Scripts;

namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// module scanning for stream commands in modules
    /// </summary>
    [Module(AutoCreate = true)]
    public class CommandModule {
        readonly IModuleContext context;
        readonly IScriptModule scripts;
        readonly IStreamModule stream;

        /// <summary>
        /// creates a new <see cref="CommandModule"/>
        /// </summary>
        /// <param name="context">context containing module information</param>
        /// <param name="scripts">access to script module</param>
        /// <param name="stream">access to stream to add commands to</param>
        public CommandModule(IModuleContext context, IScriptModule scripts, IStreamModule stream) {
            this.context = context;
            this.scripts = scripts;
            this.stream = stream;
            Task.Run((Action)ScanForCommands);
        }

        void ScanForCommands() {
            List<ModuleCommandAttribute> modulecommands = new List<ModuleCommandAttribute>();
            List<Tuple<ModuleInformation, MethodInfo, CommandAttribute>> methodcommands = new List<Tuple<ModuleInformation, MethodInfo, CommandAttribute>>();

            foreach (ModuleInformation module in context.Modules) {
                ModuleCommandAttribute[] commands = Attribute.GetCustomAttributes(module.Type, typeof(ModuleCommandAttribute)) as ModuleCommandAttribute[];
                if (commands != null)
                    foreach (ModuleCommandAttribute command in commands)
                        modulecommands.Add(command);

                foreach (MethodInfo method in module.Type.GetMethods().Where(m => Attribute.IsDefined(m, typeof(CommandAttribute))))
                    methodcommands.AddRange(CheckMethod(module, method));
            }

            foreach (ModuleCommandAttribute modulecommand in modulecommands)
                stream.RegisterCommandHandler(modulecommand.Command, modulecommand.Handler);

            foreach (Tuple<ModuleInformation, MethodInfo, CommandAttribute> command in methodcommands) {
                ScriptCommandHandler commandhandler = new ScriptCommandHandler(scripts, string.IsNullOrEmpty(command.Item1.Key) ? command.Item1.TypeName : command.Item1.Key, command.Item2.Name, false, ToScriptParameters(command.Item2.GetParameters(), command.Item3.Arguments).ToArray());
                stream.RegisterCommandHandler(command.Item3.StreamCommand, commandhandler);
            }
        }

        IEnumerable<IScriptParameter> ToScriptParameters(ParameterInfo[] parameters, object[] arguments) {
            foreach (object argument in arguments) {
                if (argument is int index)
                    yield return new IndexParameter(parameters[index].Name, index);
                else yield return new ConstantParameter($"{argument}");
            }
        }

        IEnumerable<Tuple<ModuleInformation, MethodInfo, CommandAttribute>> CheckMethod(ModuleInformation module, MethodInfo method) {
            CommandAttribute commandinfo = (CommandAttribute) Attribute.GetCustomAttribute(method, typeof(CommandAttribute));
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != commandinfo.Arguments.Length) {
                Logger.Warning(this, $"Unable to add command for {module.TypeName}::{method}", "Specified parameter count does not match");
                yield break;
            }

            yield return new Tuple<ModuleInformation, MethodInfo, CommandAttribute>(module, method, commandinfo);
        }
    }
}