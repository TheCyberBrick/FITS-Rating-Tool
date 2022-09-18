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
using FitsRatingTool.Common.Models.FitsImage;

namespace FitsRatingTool.Common.Services
{
    public interface IEvaluationService
    {
        public interface IEvaluator
        {
            string Formula { get; }

            bool IsThreadSafe { get; }

            IEvaluator Clone();


            public delegate Task EvaluationConsumer(IFitsImageStatistics statistics, IEnumerable<KeyValuePair<string, double>> variableValues, double value, CancellationToken cancellationToken = default);
            public delegate void EventConsumer(Evaluation.Event e);


            Task EvaluateAsync(IEnumerable<IFitsImageStatistics> statistics, int parallelTasks, EvaluationConsumer evaluationConsumer, EventConsumer? eventConsumer = default, CancellationToken cancellationToken = default);
        }

        bool Build(string formula, out IEvaluator? evaluator, out string? formulaErrorMessage);
    }
}
