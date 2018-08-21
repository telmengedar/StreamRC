using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NightlyCode.Core.Helpers;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace NightlyCode.StreamRC
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        readonly Context context = new Context();
        LogFileCleaner logcleaner;

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Current.DispatcherUnhandledException += OnUnhandledDispatcherException;
            Dispatcher.UnhandledException += OnUnhandledDispatcherException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            Logger.Message += new FileLogger(Path.Combine(PathExtensions.GetApplicationDirectory(), "logs/twitchrc.log")).Log;
            logcleaner = new LogFileCleaner(Path.Combine(PathExtensions.GetApplicationDirectory(), "logs"));
            logcleaner.Start(TimeSpan.FromMinutes(30.0f));

            ModuleScanner scanner=new ModuleScanner(Path.Combine(PathExtensions.GetApplicationDirectory(), "modules"));
            foreach(IModule module in scanner.ScanForModules(context))
                context.AddModule(module);

            context.Start();
            Logger.Info(this, "StreamRC started");
        }

        Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            string assemblypath = Path.Combine(PathExtensions.GetApplicationDirectory(), "modules", new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblypath))
                return null;
            return Assembly.LoadFrom(assemblypath);
        }

        void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
            Logger.Error(this, "Unobserved Task Exception", e.Exception);
        }

        void OnUnhandledDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            Logger.Error(this, "Unhandled dispatcher exception", e.Exception);
        }

        void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
            Logger.Error(this, "Unhandled application exception", e.ExceptionObject as Exception);
        }
    }
}
