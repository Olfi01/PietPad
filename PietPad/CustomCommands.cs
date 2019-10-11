using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PietPad
{
    public static class CustomCommands
    {
        public static readonly RoutedUICommand Import = new RoutedUICommand(
            "Import",
            "Import",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.I, ModifierKeys.Control)
            });
        public static readonly RoutedUICommand Export = new RoutedUICommand(
            "Export",
            "Export",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.E, ModifierKeys.Control)
            });
    }
}
