using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DryIoc;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI;
using FitsRatingTool.GuiApp.UI.App;
using FitsRatingTool.GuiApp.UI.App.Windows;

namespace FitsRatingTool.GuiApp
{
    public partial class App : Application
    {
        private Container? container;

        public override void Initialize()
        {
            container = Bootstrapper.Initialize();

            DataTemplates.Add(new ViewLocator(container));

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new AppWindow(container.Resolve<IWindowManager>(), container.Resolve<IOpenFileEventManager>())
                {
                    DataContext = container.Resolve<IAppViewModel>()
                };
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
