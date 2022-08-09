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
using static FitsRatingTool.Common.Models.FitsImage.IFitsImageStatistics;

namespace FitsRatingTool.Common.Services
{
    public interface IBatchEvaluationService
    {
        public interface ICache
        {
            void Save(string file, IEnumerable<KeyValuePair<MeasurementType, double>> stats, IEnumerable<KeyValuePair<string, string?>> groupingKeys, string? groupKey, IEnumerable<KeyValuePair<string, string>> header);

            bool Load(string file, out IDictionary<MeasurementType, double>? stats, out IDictionary<string, string?>? groupingKeys, out string? groupKey, out IDictionary<string, string>? header);

            int Delete(IEnumerable<string> files);

            int Clear();
        }


        public class InvalidConfigException : Exception
        {
            public InvalidConfigException(string? message, Exception? innerException) : base(message, innerException) { }
        }

        public class InvalidFormulaException : InvalidConfigException
        {
            public InvalidFormulaException(string? message, Exception? innerException) : base(message, innerException) { }
        }


        public delegate Task EvaluationConsumer(string file, string groupKey, IEnumerable<KeyValuePair<string, double>> variableValues, double value, CancellationToken cancellationToken = default);
        public delegate void EventConsumer(BatchEvaluation.Event e);


        public Task EvaluateAsync(IReadOnlyJobConfig jobConfig, List<string> files, EvaluationConsumer evaluationConsumer, ICache? cache = null, EventConsumer? eventConsumer = default, CancellationToken cancellationToken = default);
    }
}
