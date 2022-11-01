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
using DryIocAttributes;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.App.ViewModels;
using FitsRatingTool.GuiApp.UI.App.Windows;
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.GuiApp.UI.FitsImage.Windows;
using FitsRatingTool.GuiApp.UI.ImageAnalysis;
using FitsRatingTool.GuiApp.UI.ImageAnalysis.Windows;
using ReactiveUI;
using System;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.UI.App.Views
{
    [Export(typeof(AppViewerOverlayView)), TransientReuse]
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

                    d.Add(overlay.ShowExternalViewer.Subscribe(factory =>
                    {
                        windowManager.Show<AppExternalFitsImageViewerWindow, IAppExternalFitsImageViewerViewModel, IAppExternalFitsImageViewerViewModel.OfFile>(factory, false, out var _, w => w.DataContext is IAppExternalFitsImageViewerViewModel viewer && viewer.Selector.SelectedFile == overlay.Viewer?.File);
                    }));

                    d.Add(overlay.ShowExternalCornerViewer.Subscribe(factory =>
                    {
                        windowManager.Show<FitsImageCornerViewerWindow, IFitsImageCornerViewerViewModel, IFitsImageCornerViewerViewModel.OfViewer>(factory, false, out var _, w => w.DataContext is IFitsImageCornerViewerViewModel viewer && viewer.Viewer == overlay.Viewer);
                    }));

                    d.Add(overlay.ShowExternalImageAnalysis.Subscribe(factory =>
                    {
                        windowManager.Show<ImageAnalysisWindow, IImageAnalysisViewModel, IImageAnalysisViewModel.OfFile>(factory, false, out var _, w => w.DataContext is IImageAnalysisViewModel analysis && analysis.File == overlay.Viewer.File);
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
