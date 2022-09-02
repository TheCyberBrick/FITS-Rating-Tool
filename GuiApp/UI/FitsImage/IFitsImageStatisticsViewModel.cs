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

using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.FitsLoader.Models;

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public interface IFitsImageStatisticsViewModel : IFitsImageStatistics
    {
        public interface IFactory
        {
            IFitsImageStatisticsViewModel Create(PhotometryStatistics statistics, int stars);

            IFitsImageStatisticsViewModel Create(IFitsImageStatisticsViewModel other, int stars);
        }

        double Rating { get; set; }

        // Data grid sorting only seems to work if all
        // properties are explicitly declared in the
        // interface the data grid is bound to...
        new int Stars { get; }

        new double Median { get; }
        new double MedianMAD { get; }

        new double Noise { get; }
        new double NoiseRatio { get; }

        new double EccentricityMax { get; }
        new double EccentricityMin { get; }
        new double EccentricityMean { get; }
        new double EccentricityMedian { get; }
        new double EccentricityMAD { get; }

        new double SNRMax { get; }
        new double SNRMin { get; }
        new double SNRMean { get; }
        new double SNRMedian { get; }
        new double SNRMAD { get; }

        new double SNRWeight { get; }

        new double FWHMMax { get; }
        new double FWHMMin { get; }
        new double FWHMMean { get; }
        new double FWHMMedian { get; }
        new double FWHMMAD { get; }

        new double HFRMax { get; }
        new double HFRMin { get; }
        new double HFRMean { get; }
        new double HFRMedian { get; }
        new double HFRMAD { get; }

        new double HFDMax { get; }
        new double HFDMin { get; }
        new double HFDMean { get; }
        new double HFDMedian { get; }
        new double HFDMAD { get; }

        new double ResidualMax { get; }
        new double ResidualMin { get; }
        new double ResidualMean { get; }
        new double ResidualMedian { get; }
        new double ResidualMAD { get; }
    }
}
