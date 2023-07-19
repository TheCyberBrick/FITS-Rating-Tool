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

using FitsRatingTool.Common.Models.Evaluation;
using System.Diagnostics.CodeAnalysis;

namespace FitsRatingTool.Common.Services
{
    public interface IEvaluationExporterManager
    {
        delegate IEvaluationExporter ExporterFactory(IEvaluationExporterContext ctx, string config);


        IReadOnlyDictionary<string, ExporterFactory> Exporters { get; }

        bool Register(string id, ExporterFactory exporterFactory);

        bool Unregister(string id);

        bool TryCreateExporter(IEvaluationExporterContext ctx, string id, string config, [NotNullWhen(true)] out IEvaluationExporter? exporter);
    }

    public static class IEvaluationExporterManagerExtensions
    {
        public static bool Register(this IEvaluationExporterManager manager, string id, IEvaluationExporterFactory factory)
        {
            return manager.Register(id, factory.Create);
        }
    }
}
