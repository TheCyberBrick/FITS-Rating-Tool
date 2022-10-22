using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DryIoc;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI;
using FitsRatingTool.GuiApp.UI.App;
using FitsRatingTool.GuiApp.UI.App.Windows;
using System.Linq;

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
                IContainerRoot<IAppViewModel, IAppViewModel.Of> appContainerRoot = container.Resolve<IContainerRoot<IAppViewModel, IAppViewModel.Of>>();

                var disposable = appContainerRoot.Instantiate(new IAppViewModel.Of(), out var _, out var app);

                desktop.MainWindow = new AppWindow(container.Resolve<IWindowManager>(), container.Resolve<IOpenFileEventManager>())
                {
                    DataContext = app
                };

                desktop.MainWindow.Closed += (s, e) => disposable.Dispose();

                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
