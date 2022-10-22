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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.Services;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageHistogramViewModel : ViewModelBase, IFitsImageHistogramViewModel
    {
        public FitsImageHistogramViewModel(IRegistrar<IFitsImageHistogramViewModel, IFitsImageHistogramViewModel.OfData> reg)
        {
            reg.RegisterAndReturn<FitsImageHistogramViewModel>();
        }

        public FitsImageHistogramViewModel(IRegistrar<IFitsImageHistogramViewModel, IFitsImageHistogramViewModel.OfBuckets> reg)
        {
            reg.RegisterAndReturn<FitsImageHistogramViewModel>();
        }


        public ObservableCollection<HistogramBucket> Items { get; } = new();

        // TODO Temp
        public FitsImageHistogramViewModel(IFitsImageHistogramViewModel.OfBuckets args)
        {
            using (DelayChangeNotifications())
            {
                foreach (var bucket in args.Buckets)
                {
                    Items.Add(bucket);
                }
            }
        }

        // TODO Temp
        public FitsImageHistogramViewModel(IFitsImageHistogramViewModel.OfData args)
        {
            using (DelayChangeNotifications())
            {
                if (args.Log)
                {
                    double scaledPedestal = Math.Log(args.Histogram.Max()) * args.Pedestal;
                    for (int i = 0; i < args.Histogram.Length; ++i)
                    {
                        double value = Math.Log(args.Histogram[i]);
                        if (args.Histogram[i] > 0 && value < scaledPedestal)
                        {
                            value = scaledPedestal;
                        }
                        Items.Add(new HistogramBucket { Index = i, Value = value });
                    }
                }
                else
                {
                    double scaledPedestal = args.Histogram.Max() * args.Pedestal;
                    for (int i = 0; i < args.Histogram.Length; ++i)
                    {
                        uint value = args.Histogram[i];
                        if (value > 0 && value < scaledPedestal)
                        {
                            value = (uint)Math.Ceiling(scaledPedestal);
                        }
                        Items.Add(new HistogramBucket { Index = i, Value = value });
                    }
                }
            }
        }
    }
}
