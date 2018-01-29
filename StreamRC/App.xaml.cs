using System;
using System.IO;
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

            foreach(ModuleInformation module in context.Modules)
                if(module.Module is IMessageSender)
                    context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).RegisterMessageSender((IMessageSender)module.Module);

            context.Start();
            Logger.Info(this, "StreamRC started");
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
