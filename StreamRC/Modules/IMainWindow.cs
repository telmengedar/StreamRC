using System.Windows;
using NightlyCode.Modules;

namespace NightlyCode.StreamRC.Modules {

    /// <summary>
    /// interface for a main window
    /// </summary>
    public interface IMainWindow : IModule {

        /// <summary>
        /// adds an item to the menu bar
        /// </summary>
        /// <param name="menuname">name of menu</param>
        /// <param name="action">action to be executed when item is clicked</param>
        void AddMenuItem(string menuname, RoutedEventHandler action);

        /// <summary>
        /// adds a separator to a menu name
        /// </summary>
        /// <param name="menuname"></param>
        void AddSeparator(string menuname);
    }
}