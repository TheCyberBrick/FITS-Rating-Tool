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

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImagePSFViewModel : ViewModelBase, IFitsImagePSFViewModel
    {
        public class Factory : IFitsImagePSFViewModel.IFactory
        {
            public IFitsImagePSFViewModel Create(PhotometryObject.PSF psf)
            {
                return new FitsImagePSFViewModel(psf);
            }
        }

        private readonly PhotometryObject.PSF psf;

        private FitsImagePSFViewModel(PhotometryObject.PSF psf)
        {
            this.psf = psf;
        }

        public double X { get => psf.x + 0.5; }
        public double Y { get => psf.y + 0.5; }

        public double CanvasXMin { get => X - 1000; }
        public double CanvasXMax { get => X + 1000; }
        public double CanvasYMin { get => Y - 1000; }
        public double CanvasYMax { get => Y + 1000; }

        public double CanvasWidth { get => CanvasXMax - CanvasXMin; }
        public double CanvasHeight { get => CanvasYMax - CanvasYMin; }
        public double HalfCanvasWidth { get => CanvasWidth * 0.5; }
        public double HalfCanvasHeight { get => CanvasHeight * 0.5; }

        public double AlphaX { get => psf.alpha_x; }
        public double AlphaY { get => psf.alpha_y; }
        public double Theta { get => psf.theta; }
        public double ThetaDeg { get => 180.0 / System.Math.PI * Theta - 90.0; }

        public double Residual { get => psf.residual; }
        public double Weight { get => psf.weight; }

        public double FWHMX { get => psf.fwhm_x; }
        public double FWHMY { get => psf.fwhm_y; }
        public double FWHM { get => psf.fwhm; }
        public double FWHMX2 { get => psf.fwhm_x * 2; }
        public double FWHMY2 { get => psf.fwhm_y * 2; }
        public double FWHM2 { get => psf.fwhm * 2; }
        public double FWHMX3 { get => psf.fwhm_x * 3; }
        public double FWHMY3 { get => psf.fwhm_y * 3; }
        public double FWHM3 { get => psf.fwhm * 3; }
        public double FWHMX4 { get => psf.fwhm_x * 4; }
        public double FWHMY4 { get => psf.fwhm_y * 4; }
        public double FWHM4 { get => psf.fwhm * 4; }

        public double Eccentricity { get => psf.eccentricity; }
    }
}
