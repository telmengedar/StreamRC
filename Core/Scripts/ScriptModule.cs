using System.Linq;
using System.Reflection;
using NightlyCode.Core.Script;
using NightlyCode.Modules;
using NightlyCode.Modules.Scripts;

namespace StreamRC.Core.Scripts {

    /// <summary>
    /// provides help for scripts
    /// </summary>
    [Module(Key="scripts")]
    public class ScriptModule {
        readonly IModuleContext context;
        readonly ScriptParser scriptparser;

        /// <summary>
        /// creates a new <see cref="ScriptModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public ScriptModule(IModuleContext context) {
            this.context = context;
            scriptparser = new ScriptParser(new ContextScriptHost(context));
        }

        /// <summary>
        /// lists all available modules with a module key
        /// </summary>
        /// <remarks>
        /// these are the modules you can use in scripts by calling them by their key
        /// </remarks>
        /// <returns>list of available module keys</returns>
        public string[] ListModules() {
            return context.Modules.Where(m => !string.IsNullOrEmpty(m.Key)).Select(m => m.Key).ToArray();
        }

        string FormatMethodInfo(MethodInfo method) {
            return $"{method.Name}({string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})";
        }

        /// <summary>
        /// lists all available methods of a module
        /// </summary>
        /// <param name="modulekey">key of module of which to get available methods</param>
        /// <returns>method which can get called in scripts</returns>
        public string[] ListMethods(string modulekey) {
            object module = context.GetModuleByKey<object>(modulekey);
            return module.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(FormatMethodInfo).ToArray();
        }

        /// <summary>
        /// executes a script
        /// </summary>
        /// <param name="script">script data</param>
        /// <returns>result of executed script</returns>
        public object Execute(string script) {
            IScriptToken token = scriptparser.Parse(script);
            return token.Execute();
        }
    }
}