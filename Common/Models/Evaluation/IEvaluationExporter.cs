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
    public interface IEvaluationExporter : IDisposable
    {
        string? ConfirmationMessage { get; }

        bool ExportValue { get; set; }

        bool CanExportGroupKey { get; }

        bool ExportGroupKey { get; set; }

        bool CanExportVariables { get; }

        bool ExportVariables { get; set; }

        ISet<string> ExportVariablesFilter { get; }

        Task ExportAsync(IEvaluationExporterEventDispatcher events, string file, string groupKey, IEnumerable<KeyValuePair<string, double>> variableValues, double value, CancellationToken cancellationToken = default);

        Task FlushAsync(CancellationToken cancellationToken = default);

        void Close();
    }
}
