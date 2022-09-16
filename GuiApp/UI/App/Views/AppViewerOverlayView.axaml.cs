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

using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.App.ViewModels;
using FitsRatingTool.GuiApp.UI.FitsImage.Windows;
using ReactiveUI;
using System;

namespace FitsRatingTool.GuiApp.UI.App.Views
{
    public partial class AppViewerOverlayView : ReactiveUserControl<AppViewerOverlayViewModel>
    {
        public AppViewerOverlayView()
        {
            InitializeComponent();
        }

        public AppViewerOverlayView(IWindowManager windowManager)
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    var overlay = ViewModel;

                    d.Add(overlay.ShowExternalViewer.Subscribe(vm =>
                    {
                        windowManager.Show(() =>
                        {
                            overlay.IsExternalViewerEnabled = false;

                            var window = new FitsImageViewerWindow()
                            {
                                DataContext = vm
                            };

                            void onClosing(object? sender, EventArgs e)
                            {
                                overlay.IsExternalViewerEnabled = true;
                                window.Closing -= onClosing;
                            };
                            window.Closing += onClosing;

                            return window;
                        }, false, w => w.DataContext == vm);
                    }));

                    d.Add(overlay.ShowExternalCornerViewer.Subscribe(vm =>
                    {
                        windowManager.Show(() =>
                        {
                            overlay.IsExternalCornerViewerEnabled = false;

                            var window = new FitsImageCornerViewerWindow()
                            {
                                DataContext = vm
                            };

                            void onClosing(object? sender, EventArgs e)
                            {
                                overlay.IsExternalCornerViewerEnabled = true;
                                window.Closing -= onClosing;
                            };
                            window.Closing += onClosing;

                            return window;
                        }, false, w => w.DataContext == vm);
                    }));
                }
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
