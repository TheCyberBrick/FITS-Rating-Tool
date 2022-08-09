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
using System.Text;

namespace FitsRatingTool.Common.Utils
{
    public class BatchEvaluationProgressTracker
    {
        public enum ProgresPhase
        {
            Loading, Evaluation
        }

        public readonly struct ProgressState
        {
            public readonly int FilesTotal;
            public readonly int FilesLoaded;
            public readonly double GroupEvaluationProgress;
            public readonly int GroupCount;
            public readonly int PhaseProgress;
            public readonly double PhaseSpeedEstimate;
            public readonly ProgresPhase Phase;

            public ProgressState(int filesTotal, int filesLoaded, double groupEvaluationProgress, int groupCount, int phaseProgress, double phaseSpeedEstimate, ProgresPhase phase)
            {
                FilesTotal = filesTotal;
                FilesLoaded = filesLoaded;
                GroupEvaluationProgress = groupEvaluationProgress;
                GroupCount = groupCount;
                PhaseProgress = phaseProgress;
                PhaseSpeedEstimate = phaseSpeedEstimate;
                Phase = phase;
            }
        }



        public int FilesTotal { get; private set; }

        public int FilesLoaded { get; private set; }

        private bool evaluation = false;

        public double GroupEvaluationProgress { get; private set; }

        public int GroupCount { get; private set; }

        public int PhaseProgress { get; private set; }

        public double PhaseSpeedEstimate { get; private set; }

        public ProgresPhase Phase => evaluation ? ProgresPhase.Evaluation : ProgresPhase.Loading;



        public int RefreshInterval { get; set; } = 1000;

        public int ApproximateMinSpeedEstimateInterval { get; set; } = 100;

        public int ApproximateMinSpeedEstimateSamples { get; set; } = 8;

        private EventHandler<ProgressState>? _progressChanged;
        public event EventHandler<ProgressState> ProgressChanged
        {
            add => _progressChanged += value;
            remove => _progressChanged -= value;
        }


        private BatchEvaluationProgressTracker() { }

        public static BatchEvaluationProgressTracker Track(int totalFiles, out Task task, CancellationToken cancellationToken = default)
        {
            return Track(totalFiles, out task, 1000, cancellationToken);
        }

        public static BatchEvaluationProgressTracker Track(int totalFiles, out Task task, int refreshInterval = 1000, CancellationToken cancellationToken = default)
        {
            var tracker = new BatchEvaluationProgressTracker()
            {
                FilesTotal = totalFiles,
                RefreshInterval = refreshInterval
            };
            var progress = new Progress<ProgressState>();
            progress.ProgressChanged += (s, e) => tracker._progressChanged?.Invoke(s, e);
            task = Task.Run(async () => await tracker.ProgressLoopAsync(progress, cancellationToken));
            return tracker;
        }



        public void OnEvent(BatchEvaluation.Event e)
        {
            lock (this)
            {
                if (e.Phase == BatchEvaluation.Phase.LoadFitEnd && e is BatchEvaluation.LoadEvent le && (le.Cached || le.Skipped))
                {
                    ++FilesLoaded;
                }
                else if (e.Phase == BatchEvaluation.Phase.AnalyzeFitEnd)
                {
                    ++FilesLoaded;
                }
                else if (e.Phase == BatchEvaluation.Phase.EvaluationStepEnd && e is BatchEvaluation.EvaluationStepEvent ee)
                {
                    GroupEvaluationProgress += 1.0D / ee.StepCount / ee.GroupSize;
                    GroupCount = Math.Max(GroupCount, ee.GroupCount);
                }
            }
        }

        private async Task ProgressLoopAsync(IProgress<ProgressState> progress, CancellationToken cancellationToken = default)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                DateTime lastMeasureTime = startTime;
                DateTime lastProgressTime = startTime;

                int lastProgress = 0;
                double lastProgressDeltaTime = 0.0;
                double averageDeltaTime = 0.0;
                double measuredSpeed = 0.0;
                double averageSpeed = 0.0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    StringBuilder sb = new();

                    lock (this)
                    {
                        var currentTime = DateTime.Now;

                        if (FilesLoaded < FilesTotal)
                        {
                            PhaseProgress = FilesLoaded;
                        }
                        else
                        {
                            if (!evaluation)
                            {
                                evaluation = true;

                                lastMeasureTime = currentTime;
                                lastProgressTime = currentTime;

                                lastProgress = 0;
                                lastProgressDeltaTime = 0.0;
                                averageDeltaTime = 0.0;
                                measuredSpeed = 0.0;
                                averageSpeed = 0.0;
                            }

                            PhaseProgress = (int)Math.Round(GroupEvaluationProgress / Math.Max(GroupCount, 1) * 100);
                        }

                        int progressDiff = PhaseProgress - lastProgress;

                        double measureDeltaTime = (currentTime - lastMeasureTime).TotalSeconds;
                        if (measureDeltaTime >= averageDeltaTime * ApproximateMinSpeedEstimateSamples && measureDeltaTime > 0.0001 && measureDeltaTime >= ApproximateMinSpeedEstimateInterval / 1000.0)
                        {
                            measuredSpeed = progressDiff / measureDeltaTime;
                            lastMeasureTime = currentTime;

                            if (progressDiff > 0)
                            {
                                lastProgress = PhaseProgress;
                            }
                        }

                        if (progressDiff > 0)
                        {
                            lastProgressDeltaTime = (currentTime - lastProgressTime).TotalSeconds;
                            lastProgressTime = currentTime;
                        }

                        float averageDeltaTimeAlpha = (float)Math.Pow(0.75f, RefreshInterval / 1000.0f);
                        float averageSpeedAlpha = (float)Math.Pow(0.5f, RefreshInterval / 1000.0f);

                        averageDeltaTime = averageDeltaTimeAlpha * averageDeltaTime + (1.0f - averageDeltaTimeAlpha) * lastProgressDeltaTime;
                        averageSpeed = averageSpeedAlpha * averageSpeed + (1.0f - averageSpeedAlpha) * measuredSpeed;

                        PhaseSpeedEstimate = averageSpeed;

                        progress.Report(new ProgressState(FilesTotal, FilesLoaded, GroupEvaluationProgress, GroupCount, PhaseProgress, PhaseSpeedEstimate, Phase));
                    }

                    await Task.Delay(RefreshInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
