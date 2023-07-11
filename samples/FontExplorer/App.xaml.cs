using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace FontExplorer
{
    public class App : Application
    {
        public App()
        {
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.MainWindow = new Views.MainWindow() { 
                    DataContext = new ViewModels.MainWindowViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

    }
}
