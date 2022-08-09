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

using ReactiveUI;
using static FitsRatingTool.GuiApp.UI.FitsImage.IFitsImageSectionViewerViewModel;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageSectionViewerViewModel : ViewModelBase, IFitsImageSectionViewerViewModel
    {
        public class Factory : IFitsImageSectionViewerViewModel.IFactory
        {
            public IFitsImageSectionViewerViewModel Create(IFitsImageViewModel image)
            {
                return new FitsImageSectionViewerViewModel(image);
            }
        }


        public IFitsImageViewModel Image { get; }

        private ImageSection _section = ImageSection.Fixed(ImagePosition.TopLeft, ImagePosition.Center, 0.5, 0.5, 1.0);
        public ImageSection Section
        {
            get => _section;
            set => this.RaiseAndSetIfChanged(ref _section, value);
        }

        private FitsImageSectionViewerViewModel(IFitsImageViewModel image)
        {
            Image = image;
        }
    }
}
