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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FitsRatingTool.GuiApp.Models;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageHistogramViewModel : ViewModelBase, IFitsImageHistogramViewModel
    {
        public class Factory : IFitsImageHistogramViewModel.IFactory
        {
            public IFitsImageHistogramViewModel Create(IEnumerable<HistogramBucket> buckets)
            {
                return new FitsImageHistogramViewModel(buckets);
            }

            public IFitsImageHistogramViewModel Create(uint[] histogram, bool log = false, float pedestal = 0)
            {
                return new FitsImageHistogramViewModel(histogram, log, pedestal);
            }
        }

        public ObservableCollection<HistogramBucket> Items { get; } = new();

        private FitsImageHistogramViewModel(IEnumerable<HistogramBucket> buckets)
        {
            using (DelayChangeNotifications())
            {
                foreach (var bucket in buckets)
                {
                    Items.Add(bucket);
                }
            }
        }

        private FitsImageHistogramViewModel(uint[] histogram, bool log = false, float pedestal = 0.0f)
        {
            using (DelayChangeNotifications())
            {
                if (log)
                {
                    double scaledPedestal = Math.Log(histogram.Max()) * pedestal;
                    for (int i = 0; i < histogram.Length; ++i)
                    {
                        double value = Math.Log(histogram[i]);
                        if (histogram[i] > 0 && value < scaledPedestal)
                        {
                            value = scaledPedestal;
                        }
                        Items.Add(new HistogramBucket { Index = i, Value = value });
                    }
                }
                else
                {
                    double scaledPedestal = histogram.Max() * pedestal;
                    for (int i = 0; i < histogram.Length; ++i)
                    {
                        uint value = histogram[i];
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
