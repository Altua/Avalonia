using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace FontExplorer
{
    internal class Program
    {
        [STAThread]
        public static int Main(string[] args)
            => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        /// <summary>
        /// This method is needed for IDE previewer infrastructure
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .LogToTrace()
                .UsePlatformDetect();

    }
}
