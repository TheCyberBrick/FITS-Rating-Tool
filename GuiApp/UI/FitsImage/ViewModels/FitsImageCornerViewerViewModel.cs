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
using System;
using static FitsRatingTool.GuiApp.UI.FitsImage.IFitsImageSectionViewerViewModel;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageCornerViewerViewModel : ViewModelBase, IFitsImageCornerViewerViewModel
    {
        public class Factory : IFitsImageCornerViewerViewModel.IFactory
        {
            private readonly IFitsImageSectionViewerViewModel.IFactory fitsImageSectionFactory;

            public Factory(IFitsImageSectionViewerViewModel.IFactory fitsImageSectionFactory)
            {
                this.fitsImageSectionFactory = fitsImageSectionFactory;
            }

            public IFitsImageCornerViewerViewModel Create(IFitsImageViewerViewModel viewer)
            {
                return new FitsImageCornerViewerViewModel(viewer, fitsImageSectionFactory);
            }
        }


        public IFitsImageViewerViewModel Viewer { get; }

        private IFitsImageSectionViewerViewModel? _topSection;
        public IFitsImageSectionViewerViewModel? TopSection
        {
            get => _topSection;
            private set => this.RaiseAndSetIfChanged(ref _topSection, value);
        }

        private IFitsImageSectionViewerViewModel? _rightSection;
        public IFitsImageSectionViewerViewModel? RightSection
        {
            get => _rightSection;
            private set => this.RaiseAndSetIfChanged(ref _rightSection, value);
        }

        private IFitsImageSectionViewerViewModel? _bottomSection;
        public IFitsImageSectionViewerViewModel? BottomSection
        {
            get => _bottomSection;
            private set => this.RaiseAndSetIfChanged(ref _bottomSection, value);
        }

        private IFitsImageSectionViewerViewModel? _leftSection;
        public IFitsImageSectionViewerViewModel? LeftSection
        {
            get => _leftSection;
            private set => this.RaiseAndSetIfChanged(ref _leftSection, value);
        }

        private IFitsImageSectionViewerViewModel? _topLeftSection;
        public IFitsImageSectionViewerViewModel? TopLeftSection
        {
            get => _topLeftSection;
            private set => this.RaiseAndSetIfChanged(ref _topLeftSection, value);
        }

        private IFitsImageSectionViewerViewModel? _topRightSection;
        public IFitsImageSectionViewerViewModel? TopRightSection
        {
            get => _topRightSection;
            private set => this.RaiseAndSetIfChanged(ref _topRightSection, value);
        }

        private IFitsImageSectionViewerViewModel? _bottomLeftSection;
        public IFitsImageSectionViewerViewModel? BottomLeftSection
        {
            get => _bottomLeftSection;
            private set => this.RaiseAndSetIfChanged(ref _bottomLeftSection, value);
        }

        private IFitsImageSectionViewerViewModel? _bottomRightSection;
        public IFitsImageSectionViewerViewModel? BottomRightSection
        {
            get => _bottomRightSection;
            private set => this.RaiseAndSetIfChanged(ref _bottomRightSection, value);
        }

        private IFitsImageSectionViewerViewModel? _centerSection;
        public IFitsImageSectionViewerViewModel? CenterSection
        {
            get => _centerSection;
            private set => this.RaiseAndSetIfChanged(ref _centerSection, value);
        }

        private double _percentage = 0.125;
        public double Percentage
        {
            get => _percentage;
            set => this.RaiseAndSetIfChanged(ref _percentage, Math.Max(Math.Min(value, 1.0), 0.0001));
        }

        private FitsImageCornerViewerViewModel(IFitsImageViewerViewModel viewer, IFitsImageSectionViewerViewModel.IFactory fitsImageSectionFactory)
        {
            Viewer = viewer;

            this.WhenAnyValue(x => x.Viewer.FitsImage)
                .Subscribe(x =>
                {
                    if (x != null)
                    {
                        TopSection = fitsImageSectionFactory.Create(x);
                        RightSection = fitsImageSectionFactory.Create(x);
                        BottomSection = fitsImageSectionFactory.Create(x);
                        LeftSection = fitsImageSectionFactory.Create(x);

                        TopLeftSection = fitsImageSectionFactory.Create(x);
                        TopRightSection = fitsImageSectionFactory.Create(x);
                        BottomLeftSection = fitsImageSectionFactory.Create(x);
                        BottomRightSection = fitsImageSectionFactory.Create(x);
                        CenterSection = fitsImageSectionFactory.Create(x);

                        UpdateSections();
                    }
                    else
                    {
                        TopLeftSection = TopRightSection = BottomLeftSection = BottomRightSection = CenterSection = null;
                    }
                });

            this.WhenAnyValue(x => x.Viewer.FitsImage!.Bitmap).Subscribe(_ => UpdateSections());

            this.WhenAnyValue(x => x.Percentage).Subscribe(_ => UpdateSections());
        }

        private void UpdateSections()
        {
            var bm = Viewer.FitsImage?.Bitmap;
            if (bm != null)
            {
                var imgSize = bm.Size;

                double sizeX = imgSize.Width * Percentage;
                double sizeY = imgSize.Height * Percentage;

                if (TopSection != null)
                {
                    TopSection.Section = ImageSection.Dynamic(ImagePosition.Top, ImagePosition.Top, 0, 0, sizeX, sizeY, false);
                }

                if (RightSection != null)
                {
                    RightSection.Section = ImageSection.Dynamic(ImagePosition.Right, ImagePosition.Right, 0, 0, sizeX, sizeY, false);
                }

                if (BottomSection != null)
                {
                    BottomSection.Section = ImageSection.Dynamic(ImagePosition.Bottom, ImagePosition.Bottom, 0, 0, sizeX, sizeY, false);
                }

                if (LeftSection != null)
                {
                    LeftSection.Section = ImageSection.Dynamic(ImagePosition.Left, ImagePosition.Left, 0, 0, sizeX, sizeY, false);
                }


                if (TopLeftSection != null)
                {
                    TopLeftSection.Section = ImageSection.Dynamic(ImagePosition.TopLeft, ImagePosition.TopLeft, 0, 0, sizeX, sizeY, false);
                }

                if (TopRightSection != null)
                {
                    TopRightSection.Section = ImageSection.Dynamic(ImagePosition.TopRight, ImagePosition.TopRight, 0, 0, sizeX, sizeY, false);
                }

                if (BottomLeftSection != null)
                {
                    BottomLeftSection.Section = ImageSection.Dynamic(ImagePosition.BottomLeft, ImagePosition.BottomLeft, 0, 0, sizeX, sizeY, false);
                }

                if (BottomRightSection != null)
                {
                    BottomRightSection.Section = ImageSection.Dynamic(ImagePosition.BottomRight, ImagePosition.BottomRight, 0, 0, sizeX, sizeY, false);
                }

                if (CenterSection != null)
                {
                    CenterSection.Section = ImageSection.Dynamic(ImagePosition.Center, ImagePosition.Center, 0, 0, sizeX, sizeY, false);
                }
            }
        }
    }
}
