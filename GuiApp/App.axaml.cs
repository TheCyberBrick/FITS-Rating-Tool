/*
    FITS Rating Tool
    Copyright (C) 2022 TheCyberBrick
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DryIoc;
using FitsRatingTool.GuiApp.UI;
using FitsRatingTool.GuiApp.UI.App.Windows;
using FitsRatingTool.IoC;
using System.Reactive.Disposables;

namespace FitsRatingTool.GuiApp
{
    public partial class App : Application
    {
        private IContainer? bootstrapContainer;

        private IAppLifecycle? appLifecycle;

        public override void Initialize()
        {
            bootstrapContainer = Bootstrapper.Initialize();

            DataTemplates.Add(new ViewLocator(() => appLifecycle));

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var disposable = new CompositeDisposable
                {
                    bootstrapContainer.Resolve<IContainerRoot<IAppLifecycle, IAppLifecycle.Of>>().Instantiate(new IAppLifecycle.Of(), out appLifecycle, true),
                    appLifecycle.CreateRootDataContext(out var dataContext)
                };

                var appWindow = appLifecycle.Resolver.Resolve<AppWindow>();
                appWindow.DataContext = dataContext;

                desktop.MainWindow = appWindow;

                desktop.Exit += (s, e) => disposable.Dispose();

                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
