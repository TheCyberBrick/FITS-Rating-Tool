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
using System.Collections.Concurrent;
using System.Collections.Generic;
using FitsRatingTool.GuiApp.UI.FitsImage;

namespace FitsRatingTool.GuiApp.Repositories.Impl
{
    public class AnalysisRepository : IAnalysisRepository
    {
        private readonly ConcurrentDictionary<string, IEnumerable<IFitsImagePhotometryViewModel>> photometryRepository = new();
        private readonly ConcurrentDictionary<string, IFitsImageStatisticsViewModel> statisticsRepository = new();

        public void AddStatistics(string file, IFitsImageStatisticsViewModel statistics)
        {
            statisticsRepository.AddOrUpdate(file, statistics, (file, old) => statistics);
            _statisticsChanged?.Invoke(this, new IAnalysisRepository.RepositoryChangedEventArgs(file));
        }

        public IFitsImageStatisticsViewModel? GetStatistics(string file)
        {
            if (statisticsRepository.TryGetValue(file, out var statistics))
            {
                return statistics;
            }
            return null;
        }

        public IFitsImageStatisticsViewModel? RemoveStatistics(string file)
        {
            if (statisticsRepository.Remove(file, out var statistics))
            {
                _statisticsChanged?.Invoke(this, new IAnalysisRepository.RepositoryChangedEventArgs(file));
                return statistics;
            }
            return null;
        }

        private event EventHandler<IAnalysisRepository.RepositoryChangedEventArgs>? _statisticsChanged;
        public event EventHandler<IAnalysisRepository.RepositoryChangedEventArgs>? StatisticsChanged
        {
            add
            {
                _statisticsChanged += value;
            }
            remove
            {
                _statisticsChanged -= value;
            }
        }

        public void AddPhotometry(string file, IEnumerable<IFitsImagePhotometryViewModel> photometry)
        {
            photometry = new List<IFitsImagePhotometryViewModel>(photometry);
            photometryRepository.AddOrUpdate(file, photometry, (file, old) => photometry);
            _photometryChanged?.Invoke(this, new IAnalysisRepository.RepositoryChangedEventArgs(file));
        }

        public IEnumerable<IFitsImagePhotometryViewModel>? GetPhotometry(string file)
        {
            if (photometryRepository.TryGetValue(file, out var photometry))
            {
                return photometry;
            }
            return null;
        }

        public IEnumerable<IFitsImagePhotometryViewModel>? RemovePhotometry(string file)
        {
            if (photometryRepository.Remove(file, out var photometry))
            {
                _photometryChanged?.Invoke(this, new IAnalysisRepository.RepositoryChangedEventArgs(file));
                return photometry;
            }
            return null;
        }

        private event EventHandler<IAnalysisRepository.RepositoryChangedEventArgs>? _photometryChanged;
        public event EventHandler<IAnalysisRepository.RepositoryChangedEventArgs>? PhotometryChanged
        {
            add
            {
                _photometryChanged += value;
            }
            remove
            {
                _photometryChanged -= value;
            }
        }
    }
}
