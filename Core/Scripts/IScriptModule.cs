using NightlyCode.Core.Script;

namespace StreamRC.Core.Scripts {

    /// <summary>
    /// interface for a module providing script functionality
    /// </summary>
    public interface IScriptModule {

        /// <summary>
        /// lists all available modules with a module key
        /// </summary>
        /// <remarks>
        /// these are the modules you can use in scripts by calling them by their key
        /// </remarks>
        /// <returns>list of available module keys</returns>
        string[] ListModules();

        /// <summary>
        /// lists all available methods of a module
        /// </summary>
        /// <param name="modulekey">key of module of which to get available methods</param>
        /// <returns>method which can get called in scripts</returns>
        string[] ListMethods(string modulekey);

        /// <summary>
        /// executes a script
        /// </summary>
        /// <param name="script">script data</param>
        /// <param name="variablehost">host containing script variables</param>
        /// <returns>result of executed script</returns>
        object Execute(string script, IScriptVariableHost variablehost=null);
    }
}