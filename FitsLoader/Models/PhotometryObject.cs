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

using System.Runtime.InteropServices;

namespace FitsRatingTool.FitsLoader.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PhotometryObject
    {
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct PSF
        {
            public readonly double x;
            public readonly double y;
            public readonly double alpha_x;
            public readonly double alpha_y;
            public readonly double theta;
            public readonly double residual;
            public readonly double weight;
            public readonly double fwhm_x;
            public readonly double fwhm_y;
            public readonly double fwhm;
            public readonly double eccentricity;
        };

        public readonly int catalog_index;

        public readonly double x_min;
        public readonly double x_max;
        public readonly double y_min;
        public readonly double y_max;

        public readonly double kron_radius;
        public readonly short kron_flag;

        public readonly double ellipse_sum;
        public readonly double ellipse_sum_err;
        public readonly short ellipse_sum_flag;

        public readonly double circle_sum;
        public readonly double circle_sum_err;
        public readonly short circle_sum_flag;

        public readonly double flux;
        public readonly double flux_err;
        public readonly short flux_flag;

        public readonly double hfr;
        public readonly short hfr_flag;

        public readonly double snr;

        [MarshalAs(UnmanagedType.Struct)]
        public readonly PSF psf;
    }
}
