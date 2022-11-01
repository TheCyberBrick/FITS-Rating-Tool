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

using DryIocAttributes;
using FitsRatingTool.FitsLoader.Models;
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.ComponentModel.Composition;
using static FitsRatingTool.Common.Models.FitsImage.IFitsImageStatistics;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    [Export(typeof(IFitsImageStatisticsViewModel)), TransientReuse]
    public class FitsImageStatisticsViewModel : ViewModelBase, IFitsImageStatisticsViewModel
    {
        public FitsImageStatisticsViewModel(IRegistrar<IFitsImageStatisticsViewModel, IFitsImageStatisticsViewModel.OfStatistics> reg)
        {
            reg.RegisterAndReturn<FitsImageStatisticsViewModel>();
        }

        public FitsImageStatisticsViewModel(IRegistrar<IFitsImageStatisticsViewModel, IFitsImageStatisticsViewModel.OfOther> reg)
        {
            reg.RegisterAndReturn<FitsImageStatisticsViewModel>();
        }


        private readonly PhotometryStatistics statistics;

        // TODO Temp
        public FitsImageStatisticsViewModel(IFitsImageStatisticsViewModel.OfStatistics args)
        {
            statistics = args.Statistics;
            Stars = args.Stars;
        }

        // TODO Temp
        public FitsImageStatisticsViewModel(IFitsImageStatisticsViewModel.OfOther args)
        {
            statistics = new()
            {
                median = args.Other.Median,
                median_mad = args.Other.MedianMAD,

                noise = args.Other.Noise,
                noise_ratio = args.Other.NoiseRatio,

                eccentricity_max = args.Other.EccentricityMax,
                eccentricity_min = args.Other.EccentricityMin,
                eccentricity_mean = args.Other.EccentricityMean,
                eccentricity_median = args.Other.EccentricityMedian,
                eccentricity_mad = args.Other.EccentricityMAD,

                snr_max = args.Other.SNRMax,
                snr_min = args.Other.SNRMin,
                snr_mean = args.Other.SNRMean,
                snr_median = args.Other.SNRMedian,
                snr_mad = args.Other.SNRMAD,

                fwhm_max = args.Other.FWHMMax,
                fwhm_min = args.Other.FWHMMin,
                fwhm_mean = args.Other.FWHMMean,
                fwhm_median = args.Other.FWHMMedian,
                fwhm_mad = args.Other.FWHMMAD,

                hfr_max = args.Other.HFRMax,
                hfr_min = args.Other.HFRMin,
                hfr_mean = args.Other.HFRMean,
                hfr_median = args.Other.HFRMedian,
                hfr_mad = args.Other.HFRMAD,

                residual_max = args.Other.ResidualMax,
                residual_mean = args.Other.ResidualMean,
                residual_median = args.Other.ResidualMedian,
                residual_min = args.Other.ResidualMin,
                residual_mad = args.Other.ResidualMAD
            };
            Stars = args.Stars;
        }

        public bool GetValue(MeasurementType? measurement, out double value)
        {
            value = 0;

            switch (measurement)
            {
                case MeasurementType.Rating:
                    value = Rating;
                    return true;

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

        private double _rating;
        public double Rating
        {
            get => _rating;
            set => this.RaiseAndSetIfChanged(ref _rating, value);
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
