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

namespace FitsRatingTool.FitsLoader.Models
{
    public struct PhotometryStatistics
    {
        public double median;
        public double median_mad;

        public double noise;
        public double noise_ratio;

        public double eccentricity_max;
        public double eccentricity_min;
        public double eccentricity_mean;
        public double eccentricity_median;
        public double eccentricity_mad;

        public double snr_max;
        public double snr_min;
        public double snr_mean;
        public double snr_median;
        public double snr_mad;

        public double fwhm_max;
        public double fwhm_min;
        public double fwhm_mean;
        public double fwhm_median;
        public double fwhm_mad;

        public double hfr_max;
        public double hfr_min;
        public double hfr_mean;
        public double hfr_median;
        public double hfr_mad;

        public double residual_max;
        public double residual_min;
        public double residual_mean;
        public double residual_median;
        public double residual_mad;
    };
}
