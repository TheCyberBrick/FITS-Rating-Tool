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

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public interface IFitsImageSectionViewerViewModel
    {
        public enum ImagePosition
        {
            Top, Right, Bottom, Left,
            TopLeft, TopRight,
            BottomLeft, BottomRight,
            Center
        }

        public readonly struct ImageSection
        {
            public readonly ImagePosition Origin { get; } = ImagePosition.Center;

            public readonly ImagePosition Target { get; } = ImagePosition.Center;

            public readonly double X { get; }
            public readonly double Y { get; }

            public readonly bool IsFixedZoom { get; } = true;
            public readonly bool IsDynamicZoom => !IsFixedZoom;

            public readonly double FixedZoomMagnification { get; } = 1.0;

            public readonly double DynamicZoomSizeX { get; } = 100.0;
            public readonly double DynamicZoomSizeY { get; } = 100.0;
            public readonly bool IsDynamicZoomSizeMin { get; } = true;
            public readonly bool IsDynamicZoomSizeMax => !IsDynamicZoomSizeMax;

            private ImageSection(ImagePosition origin, ImagePosition target, double x, double y, double zoom)
            {
                Origin = origin;
                Target = target;
                X = x;
                Y = y;
                IsFixedZoom = true;
                FixedZoomMagnification = zoom;
            }

            private ImageSection(ImagePosition origin, ImagePosition target, double x, double y, double sizeX, double sizeY, bool isSizeMin)
            {
                Origin = origin;
                Target = target;
                X = x;
                Y = y;
                IsFixedZoom = false;
                DynamicZoomSizeX = sizeX;
                DynamicZoomSizeY = sizeY;
                IsDynamicZoomSizeMin = isSizeMin;
            }

            public static ImageSection Fixed(ImagePosition origin, ImagePosition target, double x, double y, double zoom)
            {
                return new ImageSection(origin, target, x, y, zoom);
            }

            public static ImageSection Dynamic(ImagePosition origin, ImagePosition target, double x, double y, double sizeX, double sizeY, bool isSizeMin)
            {
                return new ImageSection(origin, target, x, y, sizeX, sizeY, isSizeMin);
            }
        }

        public interface IFactory
        {
            IFitsImageSectionViewerViewModel Create(IFitsImageViewModel image);
        }

        IFitsImageViewModel Image { get; }

        ImageSection Section { get; set; }
    }
}
