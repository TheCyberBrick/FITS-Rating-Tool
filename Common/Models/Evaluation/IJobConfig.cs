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
    public interface IJobConfig : IReadOnlyJobConfig
    {
        new string EvaluationFormula { get; set; }

        new int ParallelTasks { get; set; }

        new int ParallelIO { get; set; }

        new IReadOnlyCollection<string>? GroupingKeys { get; set; }

        new bool GroupingKeysRequired { get; set; }

        new IReadOnlyCollection<FilterConfig>? GroupingFilters { get; set; }

        new string? OutputLogsPath { get; set; }

        new string? CachePath { get; set; }

        new long MaxImageSize { get; set; }

        new int MaxImageWidth { get; set; }

        new int MaxImageHeight { get; set; }

        new IReadOnlyCollection<ExporterConfig>? Exporters { get; set; }
    }
}
