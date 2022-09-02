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

using FitsRatingTool.FitsLoader.Models;

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public interface IFitsImagePhotometryViewModel
    {
        public interface IFactory
        {
            IFitsImagePhotometryViewModel Create(PhotometryObject obj);
        }

        int CatalogIndex { get; }

        double XMin { get; }
        double XMax { get; }
        double YMin { get; }
        double YMax { get; }

        double CanvasXMin { get; }
        double CanvasXMax { get; }
        double CanvasYMin { get; }
        double CanvasYMax { get; }

        double Width { get; }
        double Height { get; }
        double HalfWidth { get; }
        double HalfHeight { get; }

        double CanvasWidth { get; }
        double CanvasHeight { get; }
        double HalfCanvasWidth { get; }
        double HalfCanvasHeight { get; }

        double KronRadius { get; }
        double KronFlag { get; }

        double EllipseSum { get; }
        double EllipseSumErr { get; }
        short EllipseSumFlag { get; }

        double CircleSum { get; }
        double CircleSumErr { get; }
        short CircleSumFlag { get; }

        double Flux { get; }
        double FluxErr { get; }
        short FluxFlag { get; }

        double HFR { get; }
        double HFD { get; }
        short HFRFlag { get; }

        double SNR { get; }

        IFitsImagePSFViewModel PSF { get; }
    }
}
