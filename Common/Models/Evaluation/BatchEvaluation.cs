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
    public static class BatchEvaluation
    {
        public enum Phase
        {
            InitStart, InitEnd,
            LoadFitStart, LoadFitEnd,
            AnalyzeFitStart, AnalyzeFitEnd,
            CacheFitStart, CacheFitEnd,
            PrecomputeStatisticsStart, PrecomputeStatisticsEnd,
            EvaluationStepStart, EvaluationStepEnd,
            EvaluationStart, EvaluationEnd,
            ConsumeEvaluationStart, ConsumeEvaluationEnd
        }

        public class Event
        {
            public Phase Phase { get; }

            public bool Success { get; }

            public Exception? Error { get; }

            public Event(Phase phase)
            {
                Phase = phase;
                Success = true;
                Error = null;
            }

            public Event(Phase phase, Exception error)
            {
                Phase = phase;
                Success = false;
                Error = error;
            }
        }

        public class FileEvent : Event
        {
            public string File { get; }

            public int FileIndex { get; }

            public FileEvent(Phase phase, string file, int fileIndex) : base(phase)
            {
                File = file;
                FileIndex = fileIndex;
            }

            public FileEvent(Phase phase, string file, int fileIndex, Exception error) : base(phase, error)
            {
                File = file;
                FileIndex = fileIndex;
            }
        }

        public class LoadEvent : FileEvent
        {
            public bool Cached { get; }

            public bool Skipped { get; }

            public LoadEvent(Phase phase, string file, int fileIndex, bool cached, bool skipped) : base(phase, file, fileIndex)
            {
                Cached = cached;
                Skipped = skipped;
            }

            public LoadEvent(Phase phase, string file, int fileIndex, bool cached, bool skipped, Exception error) : base(phase, file, fileIndex, error)
            {
                Cached = cached;
                Skipped = skipped;
            }
        }

        public class StatisticsEvent : Event
        {
            public string Name { get; }

            public int GroupIndex { get; }

            public int GroupCount { get; }

            public string GroupKey { get; }

            public int GroupSize { get; }

            public StatisticsEvent(Phase phase, string name, int groupIndex, int groupCount, string groupKey, int groupSize) : base(phase)
            {
                Name = name;
                GroupIndex = groupIndex;
                GroupCount = groupCount;
                GroupKey = groupKey;
                GroupSize = groupSize;
            }
        }

        public class EvaluationEvent : FileEvent
        {
            public int GroupIndex { get; }

            public int GroupCount { get; }

            public string GroupKey { get; }

            public int GroupSize { get; }

            public EvaluationEvent(Phase phase, string file, int fileIndex, int groupIndex, int groupCount, string groupKey, int groupSize) : base(phase, file, fileIndex)
            {
                GroupIndex = groupIndex;
                GroupCount = groupCount;
                GroupKey = groupKey;
                GroupSize = groupSize;
            }
        }

        public class EvaluationStepEvent : EvaluationEvent
        {
            public int StepIndex { get; }

            public int StepCount { get; }

            public EvaluationStepEvent(Phase phase, string file, int fileIndex, int groupIndex, int groupCount, string groupKey, int groupSize, int stepIndex, int stepCount) : base(phase, file, fileIndex, groupIndex, groupCount, groupKey, groupSize)
            {
                StepIndex = stepIndex;
                StepCount = stepCount;
            }
        }
    }
}
