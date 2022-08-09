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

namespace FitsRatingTool.Common.Models.Evaluation
{
    public interface IReadOnlyJobConfig
    {
        // TODO Make photometry parameters configurable

        public class ExporterConfig
        {
            public string Id { get; }

            public string Config { get; }

            public ExporterConfig(string id, string config)
            {
                Id = id;
                Config = config;
            }
        }

        public class FilterConfig
        {
            public string Key { get; }

            public string Pattern { get; }

            public FilterConfig(string key, string pattern)
            {
                Key = key;
                Pattern = pattern;
            }
        }

        string EvaluationFormula { get; }

        int ParallelTasks { get; }

        int ParallelIO { get; }

        IReadOnlyCollection<string>? GroupingKeys { get; }

        bool GroupingKeysRequired { get; }

        IReadOnlyCollection<FilterConfig>? GroupingFilters { get; }

        string? OutputLogsPath { get; }

        string? CachePath { get; }

        long MaxImageSize { get; }

        int MaxImageWidth { get; }

        int MaxImageHeight { get; }

        IReadOnlyCollection<ExporterConfig>? Exporters { get; }
    }
}
