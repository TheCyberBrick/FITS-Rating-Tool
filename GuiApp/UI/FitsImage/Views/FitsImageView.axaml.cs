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

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System;
using FitsRatingTool.GuiApp.UI.FitsImage.ViewModels;

namespace FitsRatingTool.GuiApp.UI.FitsImage.Views
{
    public partial class FitsImageView : ReactiveUserControl<FitsImageViewModel>, IActivatableView
    {
        public FitsImageView()
        {
            InitializeComponent();
            var image = this.FindControl<Image>("PART_Image");
            if (image != null)
            {
                // Image does not seem to redraw when interpolation mode is changed, so it
                // is done manually here
                this.WhenActivated(d => d(this.WhenAnyValue(x => x.ViewModel!.InterpolationMode).Subscribe(x => image.InvalidateVisual())));
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
