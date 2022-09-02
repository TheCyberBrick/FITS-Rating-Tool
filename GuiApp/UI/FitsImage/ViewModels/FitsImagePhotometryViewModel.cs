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
    public class FitsImagePhotometryViewModel : ViewModelBase, IFitsImagePhotometryViewModel
    {
        public class Factory : IFitsImagePhotometryViewModel.IFactory
        {
            private readonly IFitsImagePSFViewModel.IFactory fitsImagePSFFactory;

            public Factory(IFitsImagePSFViewModel.IFactory fitsImagePSFFactory)
            {
                this.fitsImagePSFFactory = fitsImagePSFFactory;
            }

            public IFitsImagePhotometryViewModel Create(PhotometryObject obj)
            {
                return new FitsImagePhotometryViewModel(fitsImagePSFFactory, obj);
            }
        }

        private readonly PhotometryObject obj;

        private FitsImagePhotometryViewModel(IFitsImagePSFViewModel.IFactory fitsImagePSFFactory, PhotometryObject obj)
        {
            this.obj = obj;
            PSF = fitsImagePSFFactory.Create(obj.psf);
        }

        public int CatalogIndex { get => obj.catalog_index; }

        public double XMin { get => obj.x_min + 0.5; }
        public double XMax { get => obj.x_max + 0.5; }
        public double YMin { get => obj.y_min + 0.5; }
        public double YMax { get => obj.y_max + 0.5; }

        public double CanvasXMin { get => XMin - 1000; }
        public double CanvasXMax { get => XMax + 1000; }
        public double CanvasYMin { get => YMin - 1000; }
        public double CanvasYMax { get => YMax + 1000; }

        public double Width { get => XMax - XMin; }
        public double Height { get => YMax - YMin; }
        public double HalfWidth { get => Width * 0.5; }
        public double HalfHeight { get => Height * 0.5; }

        public double CanvasWidth { get => CanvasXMax - CanvasXMin; }
        public double CanvasHeight { get => CanvasYMax - CanvasYMin; }
        public double HalfCanvasWidth { get => CanvasWidth * 0.5; }
        public double HalfCanvasHeight { get => CanvasHeight * 0.5; }

        public double KronRadius { get => obj.kron_radius; }
        public double KronFlag { get => obj.kron_flag; }

        public double EllipseSum { get => obj.ellipse_sum; }
        public double EllipseSumErr { get => obj.ellipse_sum_err; }
        public short EllipseSumFlag { get => obj.ellipse_sum_flag; }

        public double CircleSum { get => obj.circle_sum; }
        public double CircleSumErr { get => obj.circle_sum_err; }
        public short CircleSumFlag { get => obj.circle_sum_flag; }

        public double Flux { get => obj.flux; }
        public double FluxErr { get => obj.flux_err; }
        public short FluxFlag { get => obj.flux_flag; }

        public double HFR { get => obj.hfr; }
        public double HFD { get => obj.hfr * 2.0; }
        public short HFRFlag { get => obj.hfr_flag; }

        public double SNR { get => obj.snr; }

        public IFitsImagePSFViewModel PSF { get; }
    }
}
