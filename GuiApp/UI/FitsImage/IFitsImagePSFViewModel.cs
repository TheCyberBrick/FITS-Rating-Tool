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
    public interface IFitsImagePSFViewModel
    {
        public interface IFactory
        {
            IFitsImagePSFViewModel Create(PhotometryObject.PSF psf);
        }

        double X { get; }
        double Y { get; }

        double CanvasXMin { get; }
        double CanvasXMax { get; }
        double CanvasYMin { get; }
        double CanvasYMax { get; }

        double CanvasWidth { get; }
        double CanvasHeight { get; }
        double HalfCanvasWidth { get; }
        double HalfCanvasHeight { get; }

        double AlphaX { get; }
        double AlphaY { get; }
        double Theta { get; }
        double ThetaDeg { get; }

        double Residual { get; }
        double Weight { get; }

        double FWHMX { get; }
        double FWHMY { get; }
        double FWHM { get; }
        double FWHMX2 { get; }
        double FWHMY2 { get; }
        double FWHM2 { get; }
        double FWHMX3 { get; }
        double FWHMY3 { get; }
        double FWHM3 { get; }
        double FWHMX4 { get; }
        double FWHMY4 { get; }
        double FWHM4 { get; }

        double Eccentricity { get; }
    }
}
