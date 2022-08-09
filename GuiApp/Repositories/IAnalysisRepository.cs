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
using FitsRatingTool.GuiApp.UI.FitsImage;

namespace FitsRatingTool.GuiApp.Repositories
{
    public interface IAnalysisRepository
    {
        public class RepositoryChangedEventArgs : EventArgs
        {
            public string File { get; private set; }

            public RepositoryChangedEventArgs(string file)
            {
                File = file;
            }
        }

        #region +++ Statistics +++
        IFitsImageStatisticsViewModel? GetStatistics(string file);

        void AddStatistics(string file, IFitsImageStatisticsViewModel statistics);

        IFitsImageStatisticsViewModel? RemoveStatistics(string file);

        event EventHandler<RepositoryChangedEventArgs> StatisticsChanged;
        #endregion

        #region +++ Photometry +++
        IEnumerable<IFitsImagePhotometryViewModel>? GetPhotometry(string file);

        void AddPhotometry(string file, IEnumerable<IFitsImagePhotometryViewModel> photometry);

        IEnumerable<IFitsImagePhotometryViewModel>? RemovePhotometry(string file);

        event EventHandler<RepositoryChangedEventArgs> PhotometryChanged;
        #endregion
    }
}
