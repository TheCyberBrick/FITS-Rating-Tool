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

using FitsRatingTool.Common.Models.FitsImage;

namespace FitsRatingTool.Common.Models.Evaluation
{
    public static class Evaluation
    {
        public enum Phase
        {
            PrecomputeStatisticsStart, PrecomputeStatisticsEnd,
            EvaluationStepStart, EvaluationStepEnd
        }

        public class Event
        {
            public Phase Phase { get; }

            public Event(Phase phase)
            {
                Phase = phase;
            }
        }

        public class StatisticsEvent : Event
        {
            public string Name { get; }

            public StatisticsEvent(Phase phase, string name) : base(phase)
            {
                Name = name;
            }
        }

        public class EvaluationStepEvent : Event
        {
            public IFitsImageStatistics Stats { get; }

            public int StepIndex { get; }

            public int StepCount { get; }

            public EvaluationStepEvent(Phase phase, int stepIndex, int stepCount, IFitsImageStatistics stats) : base(phase)
            {
                StepIndex = stepIndex;
                StepCount = stepCount;
                Stats = stats;
            }
        }
    }
}
