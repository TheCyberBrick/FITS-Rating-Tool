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
using static FitsRatingTool.Common.Models.FitsImage.IFitsImageStatistics;

namespace FitsRatingTool.Common.Services.Impl
{
    public partial class BatchEvaluationService
    {
        private class Stats : IFitsImageStatistics
        {
            private readonly PhotometryStatistics statistics;

            public Stats(PhotometryStatistics statistics, int stars)
            {
                this.statistics = statistics;
                Stars = stars;
            }

            public Stats(Stats other, int stars)
            {
                statistics = new()
                {
                    median = other.Median,
                    median_mad = other.MedianMAD,

                    noise = other.Noise,
                    noise_ratio = other.NoiseRatio,

                    eccentricity_max = other.EccentricityMax,
                    eccentricity_min = other.EccentricityMin,
                    eccentricity_mean = other.EccentricityMean,
                    eccentricity_median = other.EccentricityMedian,
                    eccentricity_mad = other.EccentricityMAD,

                    snr_max = other.SNRMax,
                    snr_min = other.SNRMin,
                    snr_mean = other.SNRMean,
                    snr_median = other.SNRMedian,
                    snr_mad = other.SNRMAD,

                    fwhm_max = other.FWHMMax,
                    fwhm_min = other.FWHMMin,
                    fwhm_mean = other.FWHMMean,
                    fwhm_median = other.FWHMMedian,
                    fwhm_mad = other.FWHMMAD,

                    hfr_max = other.HFRMax,
                    hfr_min = other.HFRMin,
                    hfr_mean = other.HFRMean,
                    hfr_median = other.HFRMedian,
                    hfr_mad = other.HFRMAD,

                    residual_max = other.ResidualMax,
                    residual_min = other.ResidualMin,
                    residual_mean = other.ResidualMean,
                    residual_median = other.ResidualMedian,
                    residual_mad = other.ResidualMAD
                };
                Stars = stars;
            }

            public Stats(IDictionary<MeasurementType, double> stats)
            {
                stats.TryGetValue(MeasurementType.Median, out statistics.median);
                stats.TryGetValue(MeasurementType.MedianMAD, out statistics.median_mad);

                stats.TryGetValue(MeasurementType.Noise, out statistics.noise);
                stats.TryGetValue(MeasurementType.NoiseRatio, out statistics.noise_ratio);

                stats.TryGetValue(MeasurementType.EccentricityMax, out statistics.eccentricity_max);
                stats.TryGetValue(MeasurementType.EccentricityMin, out statistics.eccentricity_min);
                stats.TryGetValue(MeasurementType.EccentricityMean, out statistics.eccentricity_mean);
                stats.TryGetValue(MeasurementType.EccentricityMedian, out statistics.eccentricity_median);
                stats.TryGetValue(MeasurementType.EccentricityMAD, out statistics.eccentricity_mad);

                stats.TryGetValue(MeasurementType.SNRMax, out statistics.snr_max);
                stats.TryGetValue(MeasurementType.SNRMin, out statistics.snr_min);
                stats.TryGetValue(MeasurementType.SNRMean, out statistics.snr_mean);
                stats.TryGetValue(MeasurementType.SNRMedian, out statistics.snr_median);
                stats.TryGetValue(MeasurementType.SNRMAD, out statistics.snr_mad);

                stats.TryGetValue(MeasurementType.FWHMMax, out statistics.fwhm_max);
                stats.TryGetValue(MeasurementType.FWHMMin, out statistics.fwhm_min);
                stats.TryGetValue(MeasurementType.FWHMMean, out statistics.fwhm_mean);
                stats.TryGetValue(MeasurementType.FWHMMedian, out statistics.fwhm_median);
                stats.TryGetValue(MeasurementType.FWHMMAD, out statistics.fwhm_mad);

                stats.TryGetValue(MeasurementType.HFRMax, out statistics.hfr_max);
                stats.TryGetValue(MeasurementType.HFRMin, out statistics.hfr_min);
                stats.TryGetValue(MeasurementType.HFRMean, out statistics.hfr_mean);
                stats.TryGetValue(MeasurementType.HFRMedian, out statistics.hfr_median);
                stats.TryGetValue(MeasurementType.HFRMAD, out statistics.hfr_mad);

                stats.TryGetValue(MeasurementType.ResidualMax, out statistics.residual_max);
                stats.TryGetValue(MeasurementType.ResidualMin, out statistics.residual_min);
                stats.TryGetValue(MeasurementType.ResidualMean, out statistics.residual_mean);
                stats.TryGetValue(MeasurementType.ResidualMedian, out statistics.residual_median);
                stats.TryGetValue(MeasurementType.ResidualMAD, out statistics.residual_mad);

                Stars = stats.TryGetValue(MeasurementType.Stars, out var stars) ? (int)stars : 0;
            }

            public bool GetValue(MeasurementType? measurement, out double value)
            {
                value = 0;

                switch (measurement)
                {
                    case MeasurementType.Stars:
                        value = Stars;
                        return true;

                    case MeasurementType.Median:
                        value = Median;
                        return true;
                    case MeasurementType.MedianMAD:
                        value = MedianMAD;
                        return true;

                    case MeasurementType.Noise:
                        value = Noise;
                        return true;
                    case MeasurementType.NoiseRatio:
                        value = NoiseRatio;
                        return true;

                    case MeasurementType.EccentricityMax:
                        value = EccentricityMax;
                        return true;
                    case MeasurementType.EccentricityMin:
                        value = EccentricityMin;
                        return true;
                    case MeasurementType.EccentricityMean:
                        value = EccentricityMean;
                        return true;
                    case MeasurementType.EccentricityMedian:
                        value = EccentricityMedian;
                        return true;
                    case MeasurementType.EccentricityMAD:
                        value = EccentricityMAD;
                        return true;

                    case MeasurementType.SNRMax:
                        value = SNRMax;
                        return true;
                    case MeasurementType.SNRMin:
                        value = SNRMin;
                        return true;
                    case MeasurementType.SNRMean:
                        value = SNRMean;
                        return true;
                    case MeasurementType.SNRMedian:
                        value = SNRMedian;
                        return true;
                    case MeasurementType.SNRMAD:
                        value = SNRMAD;
                        return true;

                    case MeasurementType.SNRWeight:
                        value = SNRWeight;
                        return true;

                    case MeasurementType.HFDMax:
                        value = HFDMax;
                        return true;
                    case MeasurementType.HFDMin:
                        value = HFDMin;
                        return true;
                    case MeasurementType.HFDMean:
                        value = HFDMean;
                        return true;
                    case MeasurementType.HFDMedian:
                        value = HFDMedian;
                        return true;
                    case MeasurementType.HFDMAD:
                        value = HFDMAD;
                        return true;

                    case MeasurementType.HFRMax:
                        value = HFRMax;
                        return true;
                    case MeasurementType.HFRMin:
                        value = HFRMin;
                        return true;
                    case MeasurementType.HFRMean:
                        value = HFRMean;
                        return true;
                    case MeasurementType.HFRMedian:
                        value = HFRMedian;
                        return true;
                    case MeasurementType.HFRMAD:
                        value = HFRMAD;
                        return true;

                    case MeasurementType.FWHMMax:
                        value = FWHMMax;
                        return true;
                    case MeasurementType.FWHMMin:
                        value = FWHMMin;
                        return true;
                    case MeasurementType.FWHMMean:
                        value = FWHMMean;
                        return true;
                    case MeasurementType.FWHMMedian:
                        value = FWHMMedian;
                        return true;
                    case MeasurementType.FWHMMAD:
                        value = FWHMMAD;
                        return true;

                    case MeasurementType.ResidualMax:
                        value = ResidualMax;
                        return true;
                    case MeasurementType.ResidualMin:
                        value = ResidualMin;
                        return true;
                    case MeasurementType.ResidualMean:
                        value = ResidualMean;
                        return true;
                    case MeasurementType.ResidualMedian:
                        value = ResidualMedian;
                        return true;
                    case MeasurementType.ResidualMAD:
                        value = ResidualMAD;
                        return true;
                }

                return false;
            }

            public int Stars { get; }

            public double Median { get => statistics.median; }
            public double MedianMAD { get => statistics.median_mad; }

            public double Noise { get => statistics.noise; }
            public double NoiseRatio { get => statistics.noise_ratio; }

            public double EccentricityMax { get => statistics.eccentricity_max; }
            public double EccentricityMin { get => statistics.eccentricity_min; }
            public double EccentricityMean { get => statistics.eccentricity_mean; }
            public double EccentricityMedian { get => statistics.eccentricity_median; }
            public double EccentricityMAD { get => statistics.eccentricity_mad; }

            public double SNRMax { get => statistics.snr_max; }
            public double SNRMin { get => statistics.snr_min; }
            public double SNRMean { get => statistics.snr_mean; }
            public double SNRMedian { get => statistics.snr_median; }
            public double SNRMAD { get => statistics.snr_mad; }

            public double SNRWeight { get => Math.Pow(MedianMAD / Noise, 2); }

            public double FWHMMax { get => statistics.fwhm_max; }
            public double FWHMMin { get => statistics.fwhm_min; }
            public double FWHMMean { get => statistics.fwhm_mean; }
            public double FWHMMedian { get => statistics.fwhm_median; }
            public double FWHMMAD { get => statistics.fwhm_mad; }

            public double HFRMax { get => statistics.hfr_max; }
            public double HFRMin { get => statistics.hfr_min; }
            public double HFRMean { get => statistics.hfr_mean; }
            public double HFRMedian { get => statistics.hfr_median; }
            public double HFRMAD { get => statistics.hfr_mad; }

            public double HFDMax { get => HFRMax * 2; }
            public double HFDMin { get => HFRMin * 2; }
            public double HFDMean { get => HFRMedian * 2; }
            public double HFDMedian { get => HFRMedian * 2; }
            public double HFDMAD { get => HFRMAD * 2; }

            public double ResidualMax { get => statistics.residual_max; }
            public double ResidualMin { get => statistics.residual_min; }
            public double ResidualMean { get => statistics.residual_mean; }
            public double ResidualMedian { get => statistics.residual_median; }
            public double ResidualMAD { get => statistics.residual_mad; }
        }
    }
}
