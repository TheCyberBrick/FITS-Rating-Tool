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
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FitsRatingTool.GuiApp.UI.FitsImage.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace FitsRatingTool.GuiApp.UI.FitsImage.Windows
{
    public partial class FitsImageCornerViewerWindow : ReactiveWindow<FitsImageCornerViewerViewModel>
    {
        public FitsImageCornerViewerWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            var defaultTitle = Title;

            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Viewer.FileName).Subscribe(x =>
                {
                    Title = x ?? defaultTitle;
                }).DisposeWith(d);
            });

            PointerWheelChanged += (s, e) =>
            {
                if(ViewModel != null)
                {
                    ViewModel.Percentage = Math.Min(0.5, ViewModel.Percentage - (Math.Pow(1.2, e.Delta.Y) - 1.0) * 0.05);
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
