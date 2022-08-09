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

using Avalonia.Collections;
using ReactiveUI;
using System.Reactive;
using FitsRatingTool.GuiApp.Models;

namespace FitsRatingTool.GuiApp.UI.Evaluation
{
    public interface IJobGroupingConfiguratorViewModel
    {
        public interface IFactory
        {
            public IJobGroupingConfiguratorViewModel Create(GroupingConfiguration? configuration = null);
        }

        bool IsGroupedByObject { get; set; }

        bool IsGroupedByFilter { get; set; }

        bool IsGroupedByExposureTime { get; set; }

        bool IsGroupedByGainAndOffset { get; set; }

        bool IsGroupedByParentDir { get; set; }

        bool IsGroupedByFitsKeyword { get; set; }

        int GroupingParentDirs { get; set; }

        bool IsSingleGroupingParentDir { get; }

        ReactiveCommand<Unit, Unit> IncreaseGroupingParentDirs { get; }

        ReactiveCommand<Unit, Unit> DecreaseGroupingParentDirs { get; }

        public interface IGroupingFitsKeywordViewModel
        {
            ReactiveCommand<Unit, Unit> Remove { get; }

            string? Keyword { get; set; }
        }

        AvaloniaList<IGroupingFitsKeywordViewModel> GroupingFitsKeywords { get; }

        ReactiveCommand<Unit, Unit> AddNewGroupingFitsKeyword { get; }

        void AddGroupingFitsKeyword(string? keyword);

        void ClearGroupingFitsKeywords();

        GroupingConfiguration GroupingConfiguration { get; }

        GroupingConfiguration GetGroupingConfiguration();
    }
}
