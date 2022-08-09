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

namespace FitsRatingTool.Common.Services
{
    public interface IStandaloneEvaluationService
    {
        public class InvalidConfigException : Exception
        {
            public InvalidConfigException(string? message, Exception? innerException) : base(message, innerException) { }
        }

        public class InvalidExporterException : InvalidConfigException
        {
            public InvalidExporterException(string? message, Exception? innerException) : base(message, innerException) { }
        }


        public delegate IEvaluationExporter ExporterFactory(IEvaluationExporterContext ctx, string config);


        public IReadOnlyCollection<string> Exporters { get; }

        public bool RegisterExporter(string id, ExporterFactory exporterFactory);

        public bool UnregisterExporter(string id);


        public delegate Task EvaluationConsumer(string file, string groupKey, IEnumerable<KeyValuePair<string, double>> variableValues, double value, CancellationToken cancellationToken = default);
        public delegate void EventConsumer(BatchEvaluation.Event e);


        Task EvaluateAsync(string jobConfigFile, List<string> files, EvaluationConsumer? evaluationConsumer = null, EventConsumer? eventConsumer = null, Action<string?>? logFileConsumer = null, CancellationToken cancellationToken = default);


        string? GetCachePath(string jobConfigFile);

        int DeleteCache(string jobConfigFile, IEnumerable<string> file);

        int ClearCache(string jobConfigFile);
    }
}
