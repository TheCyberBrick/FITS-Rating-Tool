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
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.GuiApp.UI.FitsImage.Windows;
using FitsRatingTool.GuiApp.UI.ImageAnalysis;
using FitsRatingTool.GuiApp.UI.ImageAnalysis.Windows;
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

                    d.Add(overlay.ShowExternalViewer.Subscribe(instantiator =>
                    {
                        windowManager.Show<FitsImageViewerWindow, IFitsImageViewerViewModel, IFitsImageViewerViewModel.Of>(instantiator, false, out var _, w => w.DataContext is IFitsImageViewerViewModel viewer && viewer.File == overlay.Viewer.File);
                    }));

                    d.Add(overlay.ShowExternalCornerViewer.Subscribe(instantiator =>
                    {
                        windowManager.Show<FitsImageCornerViewerWindow, IFitsImageCornerViewerViewModel, IFitsImageCornerViewerViewModel.OfViewer>(instantiator, false, out var _, w => w.DataContext is IFitsImageCornerViewerViewModel viewer && viewer.Viewer == overlay.Viewer);
                    }));

                    d.Add(overlay.ShowExternalImageAnalysis.Subscribe(instantiator =>
                    {
                        windowManager.Show<ImageAnalysisWindow, IImageAnalysisViewModel, IImageAnalysisViewModel.OfFile>(instantiator, false, out var _, w => w.DataContext is IImageAnalysisViewModel analysis && analysis.File == overlay.Viewer.File);
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
