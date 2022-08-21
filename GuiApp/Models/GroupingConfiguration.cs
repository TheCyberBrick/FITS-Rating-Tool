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
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.Models
{
    public class GroupingConfiguration
    {
        public bool IsGroupedByObject { get; }

        public bool IsGroupedByFilter { get; }

        public bool IsGroupedByExposureTime { get; }

        public bool IsGroupedByGainAndOffset { get; }

        public bool IsGroupedByParentDir { get; }

        public bool IsGroupedByFitsKeyword { get; }

        public int GroupingParentDirs { get; }

        public IReadOnlyList<string>? GroupingFitsKeywords { get; }

        private IReadOnlyList<string>? _groupingKeys;
        public IReadOnlyList<string> GroupingKeys
        {
            get
            {
                if (_groupingKeys == null)
                {
                    _groupingKeys = GetGroupingKeys();
                }
                return _groupingKeys;
            }
        }

        public GroupingConfiguration(
            bool isGroupedByObject, bool isGroupedByFilter, bool isGroupedByExposureTime,
            bool isGroupedByGainAndOffset, bool isGroupedByParentDir, bool isGroupedByFitsKeyword, int groupingParentDirs,
            IEnumerable<string>? groupingFitsKeywords)
        {
            IsGroupedByObject = isGroupedByObject;
            IsGroupedByFilter = isGroupedByFilter;
            IsGroupedByExposureTime = isGroupedByExposureTime;
            IsGroupedByGainAndOffset = isGroupedByGainAndOffset;
            IsGroupedByParentDir = isGroupedByParentDir;
            IsGroupedByFitsKeyword = isGroupedByFitsKeyword;
            GroupingParentDirs = isGroupedByParentDir ? groupingParentDirs : 0;
            var groupingFitsKeywordsCopy = groupingFitsKeywords != null && isGroupedByFitsKeyword ? new List<string>(groupingFitsKeywords) : null;
            if (groupingFitsKeywordsCopy != null && groupingFitsKeywordsCopy.Count == 0)
            {
                groupingFitsKeywordsCopy = null;
            }
            GroupingFitsKeywords = groupingFitsKeywordsCopy;
        }

        public static bool TryParseGroupingKeys(IGroupingManager groupingManager, IEnumerable<string> groupingKeys, out GroupingConfiguration? groupingConfiguration)
        {
            groupingConfiguration = null;

            bool isGroupedByObject = false;
            bool isGroupedByFilter = false;
            bool isGroupedByExposureTime = false;
            bool isGroupedByGain = false;
            bool isGroupedByOffset = false;
            bool isGroupedByParentDir = false;
            bool isGroupedByFitsKeyword = false;
            int groupingParentDirs = 0;
            List<string> groupingFitsKeywords = new();

            foreach (var groupingKey in groupingKeys)
            {
                if (groupingManager.TryParseGroupingKey(groupingKey, out var key, out var args))
                {
                    switch (key)
                    {
                        case "Object":
                            isGroupedByObject = true;
                            break;
                        case "Filter":
                            isGroupedByFilter = true;
                            break;
                        case "ExposureTime":
                            isGroupedByExposureTime = true;
                            break;
                        case "Gain":
                            isGroupedByGain = true;
                            break;
                        case "Offset":
                            isGroupedByOffset = true;
                            break;
                        case "ParentDir":
                            isGroupedByParentDir = true;
                            groupingParentDirs = 1;
                            break;
                        case "ParentDirs":
                            isGroupedByParentDir = true;
                            groupingParentDirs = (int)args[0];
                            break;
                        case "Keyword":
                            isGroupedByFitsKeyword = true;
                            groupingFitsKeywords.Add((string)args[0]);
                            break;
                    }
                }
                else
                {
                    return false;
                }
            }

            groupingConfiguration = new GroupingConfiguration(
                isGroupedByObject, isGroupedByFilter, isGroupedByExposureTime,
                isGroupedByGain && isGroupedByOffset, isGroupedByParentDir,
                isGroupedByFitsKeyword, groupingParentDirs, groupingFitsKeywords);

            return true;
        }

        private List<string> GetGroupingKeys()
        {
            var groupingKeys = new List<string>();
            if (IsGroupedByObject)
            {
                groupingKeys.Add("Object");
            }
            if (IsGroupedByFilter)
            {
                groupingKeys.Add("Filter");
            }
            if (IsGroupedByExposureTime)
            {
                groupingKeys.Add("ExposureTime");
            }
            if (IsGroupedByGainAndOffset)
            {
                groupingKeys.Add("Gain");
                groupingKeys.Add("Offset");
            }
            if (IsGroupedByParentDir)
            {
                groupingKeys.Add("ParentDirs:" + GroupingParentDirs);
            }
            if (IsGroupedByFitsKeyword && GroupingFitsKeywords != null)
            {
                foreach (var key in GroupingFitsKeywords)
                {
                    groupingKeys.Add("Keyword:" + (key ?? ""));
                }
            }
            return groupingKeys;
        }

        public override bool Equals(object? obj)
        {
            return obj is GroupingConfiguration configuration &&
                   IsGroupedByObject == configuration.IsGroupedByObject &&
                   IsGroupedByFilter == configuration.IsGroupedByFilter &&
                   IsGroupedByExposureTime == configuration.IsGroupedByExposureTime &&
                   IsGroupedByGainAndOffset == configuration.IsGroupedByGainAndOffset &&
                   IsGroupedByParentDir == configuration.IsGroupedByParentDir &&
                   IsGroupedByFitsKeyword == configuration.IsGroupedByFitsKeyword &&
                   GroupingParentDirs == configuration.GroupingParentDirs &&
                   EqualityComparer<IReadOnlyList<string>?>.Default.Equals(GroupingFitsKeywords, configuration.GroupingFitsKeywords);
        }

        public override int GetHashCode()
        {
            System.HashCode hash = new System.HashCode();
            hash.Add(IsGroupedByObject);
            hash.Add(IsGroupedByFilter);
            hash.Add(IsGroupedByExposureTime);
            hash.Add(IsGroupedByGainAndOffset);
            hash.Add(IsGroupedByParentDir);
            hash.Add(IsGroupedByFitsKeyword);
            hash.Add(GroupingParentDirs);
            hash.Add(GroupingFitsKeywords);
            return hash.ToHashCode();
        }
    }
}
