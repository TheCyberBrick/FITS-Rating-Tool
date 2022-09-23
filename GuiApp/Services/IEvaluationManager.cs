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

using FitsRatingTool.Common.Services;
using System;
using FitsRatingTool.GuiApp.Models;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IEvaluationManager
    {
        public class GroupingChangedEventArgs : EventArgs
        {
            public IGroupingManager.IGrouping? OldGrouping { get; }

            public IGroupingManager.IGrouping? NewGrouping { get; }

            public GroupingChangedEventArgs(IGroupingManager.IGrouping? oldGrouping, IGroupingManager.IGrouping? newGrouping)
            {
                OldGrouping = oldGrouping;
                NewGrouping = newGrouping;
            }
        }

        public class GroupKeyChangedEventArgs : EventArgs
        {
            public string? OldGroupKey { get; }

            public string? NewGroupKey { get; }

            public GroupKeyChangedEventArgs(string? oldGroupKey, string? newGroupKey)
            {
                OldGroupKey = oldGroupKey;
                NewGroupKey = newGroupKey;
            }
        }

        public class GroupingConfigurationChangedEventArgs : EventArgs
        {
            public GroupingConfiguration? OldGroupingConfiguration { get; }

            public GroupingConfiguration? NewGroupingConfiguration { get; }

            public GroupingConfigurationChangedEventArgs(GroupingConfiguration? oldGroupingConfiguration, GroupingConfiguration? newGroupingConfiguration)
            {
                OldGroupingConfiguration = oldGroupingConfiguration;
                NewGroupingConfiguration = newGroupingConfiguration;
            }
        }

        public class FormulaChangedEventArgs : EventArgs
        {
            public string? OldFormula { get; }

            public string? NewFormula { get; }

            public FormulaChangedEventArgs(string? oldFormula, string? newFormula)
            {
                OldFormula = oldFormula;
                NewFormula = newFormula;
            }
        }



        IGroupingManager.IGrouping? CurrentGrouping { get; }

        IGroupingManager.IGrouping? CurrentFilterGrouping { get; }

        string? CurrentFilterGroupKey { get; set; }

        GroupingConfiguration? CurrentGroupingConfiguration { get; set; }

        GroupingConfiguration? CurrentFilterGroupingConfiguration { get; set; }

        string? CurrentFormula { get; set; }

        bool AutoUpdateRatings { get; set; }



        Task UpdateRatingsAsync(string? specificGroupKey = null, IEvaluationService.IEvaluator? evaluator = null);



        event EventHandler<GroupingChangedEventArgs> CurrentGroupingChanged;

        event EventHandler<GroupKeyChangedEventArgs> CurrentFilterGroupKeyChanged;

        event EventHandler<GroupingConfigurationChangedEventArgs> CurrentGroupingConfigurationChanged;

        event EventHandler<GroupingConfigurationChangedEventArgs> CurrentFilterGroupingConfigurationChanged;

        event EventHandler<FormulaChangedEventArgs> CurrentFormulaChanged;
    }
}
