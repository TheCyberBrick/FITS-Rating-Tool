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

namespace FitsRatingTool.Common.Models.FitsImage
{
    public interface IFitsImageStatistics
    {
        public enum MeasurementType
        {
            Stars,
            Median, MedianMAD,
            Noise, NoiseRatio,
            EccentricityMax, EccentricityMin, EccentricityMean, EccentricityMedian, EccentricityMAD,
            SNRMax, SNRMin, SNRMean, SNRMedian, SNRMAD,
            SNRWeight,
            FWHMMax, FWHMMin, FWHMMean, FWHMMedian, FWHMMAD,
            HFRMax, HFRMin, HFRMean, HFRMedian, HFRMAD,
            HFDMax, HFDMin, HFDMean, HFDMedian, HFDMAD,
            ResidualMax, ResidualMin, ResidualMean, ResidualMedian, ResidualMAD,
            Rating
        }

        bool GetValue(MeasurementType? measurement, out double value);

        int Stars { get; }

        double Median { get; }
        double MedianMAD { get; }

        double Noise { get; }
        double NoiseRatio { get; }

        double EccentricityMax { get; }
        double EccentricityMin { get; }
        double EccentricityMean { get; }
        double EccentricityMedian { get; }
        double EccentricityMAD { get; }

        double SNRMax { get; }
        double SNRMin { get; }
        double SNRMean { get; }
        double SNRMedian { get; }
        double SNRMAD { get; }

        double SNRWeight { get; }

        double FWHMMax { get; }
        double FWHMMin { get; }
        double FWHMMean { get; }
        double FWHMMedian { get; }
        double FWHMMAD { get; }

        double HFRMax { get; }
        double HFRMin { get; }
        double HFRMean { get; }
        double HFRMedian { get; }
        double HFRMAD { get; }

        double HFDMax { get; }
        double HFDMin { get; }
        double HFDMean { get; }
        double HFDMedian { get; }
        double HFDMAD { get; }

        double ResidualMax { get; }
        double ResidualMin { get; }
        double ResidualMean { get; }
        double ResidualMedian { get; }
        double ResidualMAD { get; }
    }
}
