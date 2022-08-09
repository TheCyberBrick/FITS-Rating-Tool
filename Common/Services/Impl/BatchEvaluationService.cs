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
using FitsRatingTool.Common.Utils;
using Microsoft.VisualStudio.Threading;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using static FitsRatingTool.Common.Models.FitsImage.IFitsImageStatistics;

namespace FitsRatingTool.Common.Services.Impl
{
    public partial class BatchEvaluationService : IBatchEvaluationService
    {
        private class Filters
        {
            public IGroupingManager.IGrouping Grouping { get; }

            public Dictionary<string, Regex> Patterns { get; }

            public Filters(IGroupingManager.IGrouping grouping, Dictionary<string, Regex> patterns)
            {
                Grouping = grouping;
                Patterns = patterns;
            }

            public bool ShouldLoadFile(string file, Func<string, string?> header)
            {
                var groupMatch = Grouping.GetGroupMatch(file, header);

                foreach (var pair in Patterns)
                {
                    var groupKey = pair.Key;
                    var pattern = pair.Value;

                    var groupKeyValue = groupMatch?.Matches?.GetValueOrDefault(groupKey, null) ?? "";

                    if (!pattern.IsMatch(groupKeyValue))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private readonly IFitsImageLoader imageLoader;
        private readonly IGroupingManager groupingManager;
        private readonly IEvaluationService evaluationService;

        public BatchEvaluationService(IFitsImageLoader imageLoader, IGroupingManager groupingManager, IEvaluationService evaluationService)
        {
            this.imageLoader = imageLoader;
            this.groupingManager = groupingManager;
            this.evaluationService = evaluationService;
        }

        private bool ShouldLoadFile(string file, IReadOnlyJobConfig jobConfig, Filters? filters, IGroupingManager.IGrouping grouping, IGroupingManager.IGroupMatch? groupMatch, Func<string, string?> header)
        {
            if (jobConfig.GroupingKeysRequired)
            {
                if (groupMatch != null)
                {
                    foreach (var value in groupMatch.Matches.Values)
                    {
                        if (value == null)
                        {
                            return false;
                        }
                    }
                }
                else if (grouping.Keys.Count > 0)
                {
                    return false;
                }
            }

            if (filters != null)
            {
                return filters.ShouldLoadFile(file, header);
            }

            return true;
        }

        private async Task<Tuple<IFitsImageStatistics, string?>?> LoadAndCalculateStatisticsAsync(string file, IReadOnlyJobConfig jobConfig, Filters? filters, AsyncSemaphore ioThrottle, int index, IGroupingManager.IGrouping grouping, IBatchEvaluationService.ICache? cache, IBatchEvaluationService.EventConsumer? eventConsumer = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IFitsImageStatistics? stats = null;
            string? groupKey = null;

            try
            {
                eventConsumer?.Invoke(new BatchEvaluation.LoadEvent(BatchEvaluation.Phase.LoadFitStart, file, index, false, false));

                Exception? cacheLoadException = null;

                IGroupingManager.IGroupMatch? groupMatch = null;

                // Check if data is already available in the cache
                try
                {
                    // Check if cache is available and file is cached
                    if (cache != null && cache.Load(file, out var cachedStats, out var cachedGroupingKeys, out var cachedGroupKey, out var cachedHeader) && cachedStats != null && cachedGroupingKeys != null && cachedHeader != null)
                    {
                        bool isSameGrouping = true;
                        HashSet<string> keys = new(grouping.Keys);
                        foreach (var key in cachedGroupingKeys.Keys)
                        {
                            if (!keys.Remove(key))
                            {
                                isSameGrouping = false;
                                break;
                            }
                        }
                        isSameGrouping &= keys.Count == 0;

                        // Group key needs to be reevaluated if
                        // grouping is no longer the same and then
                        // afterwards be saved to the cache again
                        if (!isSameGrouping)
                        {
                            groupMatch = grouping.GetGroupMatch(file, keyword =>
                            {
                                if (cachedHeader.TryGetValue(keyword, out var value))
                                {
                                    return value;
                                }
                                return null;
                            });
                            cachedGroupKey = groupMatch != null ? groupMatch.GroupKey : null;

                            cache.Save(file, cachedStats, groupMatch != null ? groupMatch.Matches : new List<KeyValuePair<string, string?>>(), cachedGroupKey, cachedHeader);
                        }
                        else
                        {
                            groupMatch = grouping.GetGroupMatch(key =>
                            {
                                cachedGroupingKeys.TryGetValue(key, out var value);
                                return value;
                            });
                        }

                        // Check if file should be loaded, otherwise skip
                        if (!ShouldLoadFile(file, jobConfig, filters, grouping, groupMatch, keyword =>
                        {
                            if (cachedHeader.TryGetValue(keyword, out var value))
                            {
                                return value;
                            }
                            return null;
                        }))
                        {
                            eventConsumer?.Invoke(new BatchEvaluation.LoadEvent(BatchEvaluation.Phase.LoadFitEnd, file, index, true, true));
                            return null;
                        }

                        bool hasAllMeasurements = true;
                        stats = new Stats(cachedStats);

                        foreach (var measurement in Enum.GetValues<MeasurementType>())
                        {
                            // GetValue returns whether the stats implements the specified measurement type.
                            // So, if that returns true and the measurement is not contained in cachedStats,
                            // then that means that not all measurements are cached -> needs to be reanalyzed
                            if (stats!.GetValue(measurement, out var _) && !cachedStats.ContainsKey(measurement))
                            {
                                hasAllMeasurements = false;
                                break;
                            }
                        }

                        // If all measurements are cached, then we can skip the analyze step. Otherwise,
                        // the image needs to be analyzed again
                        if (hasAllMeasurements)
                        {
                            eventConsumer?.Invoke(new BatchEvaluation.LoadEvent(BatchEvaluation.Phase.LoadFitEnd, file, index, true, false));

                            return Tuple.Create((IFitsImageStatistics)new Stats(cachedStats), cachedGroupKey);
                        }
                    }
                }
                catch (Exception ex)
                {
                    cacheLoadException = new Exception("Failed loading data from cache", ex);
                }

                IFitsImage? image = null;

                using (await ioThrottle.EnterAsync(cancellationToken))
                {
                    // Load FITS file info & header
                    image = imageLoader.LoadFit(file, jobConfig.MaxImageSize, jobConfig.MaxImageWidth, jobConfig.MaxImageHeight);

                    if (image != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Find group match
                        groupMatch = grouping.GetGroupMatch(file, keyword =>
                        {
                            if (image.Header.TryGetValue(keyword, out var header))
                            {
                                return header.Value;
                            }
                            return null;
                        });
                        groupKey = groupMatch != null ? groupMatch.GroupKey : null;

                        // And check if file should be loaded, otherwise skip
                        if (!ShouldLoadFile(file, jobConfig, filters, grouping, groupMatch, keyword =>
                        {
                            if (image.Header.TryGetValue(keyword, out var header))
                            {
                                return header.Value;
                            }
                            return null;
                        }))
                        {
                            // Skip
                            eventConsumer?.Invoke(new BatchEvaluation.LoadEvent(BatchEvaluation.Phase.LoadFitEnd, file, index, false, true));

                            if (cache != null)
                            {
                                // But still cache all info except for measurements
                                try
                                {
                                    eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.CacheFitStart, file, index));

                                    IEnumerable<KeyValuePair<string, string>> headerEnumerable()
                                    {
                                        foreach (var record in image.Header)
                                        {
                                            yield return KeyValuePair.Create(record.Key, record.Value.Value);
                                        }
                                    }

                                    cache.Save(file, new Dictionary<MeasurementType, double>() /* no stats to cache */, groupMatch != null ? groupMatch.Matches : new List<KeyValuePair<string, string?>>(), groupKey, headerEnumerable());

                                    eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.CacheFitEnd, file, index));
                                }
                                catch (Exception ex)
                                {
                                    eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.CacheFitEnd, file, index, ex));
                                }
                            }

                            return null;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        // Load image data for analyzing later on
                        image.LoadImageData(new() { monoColorOutline = false, saturation = 1.0f });

                        cancellationToken.ThrowIfCancellationRequested();

                        // All data read, can close file at this point
                        image.CloseFile();
                    }
                }

                if (image != null)
                {
                    try
                    {
                        if (cacheLoadException != null)
                        {
                            eventConsumer?.Invoke(new BatchEvaluation.LoadEvent(BatchEvaluation.Phase.LoadFitEnd, file, index, false, false, cacheLoadException));
                        }
                        else
                        {
                            eventConsumer?.Invoke(new BatchEvaluation.LoadEvent(BatchEvaluation.Phase.LoadFitEnd, file, index, false, false));
                        }

                        try
                        {
                            eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.AnalyzeFitStart, file, index));

                            // Calculate statistics and photometry
                            if (image.ComputeStatisticsAndPhotometry() && image.GetStatistics(out var loaderImageStatistics))
                            {
                                image.GetPhotometry(out var loaderImagePhotometry);
                                stats = new Stats(loaderImageStatistics, loaderImagePhotometry != null ? loaderImagePhotometry.Length : 0);
                            }
                            else
                            {
                                throw new Exception("Failed loading image statistics");
                            }

                            eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.AnalyzeFitEnd, file, index));

                            if (cache != null)
                            {
                                // Cache all info and measurements
                                try
                                {
                                    eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.CacheFitStart, file, index));

                                    IEnumerable<KeyValuePair<MeasurementType, double>> cachedStatsEnumerable()
                                    {
                                        foreach (var measurement in Enum.GetValues<MeasurementType>())
                                        {
                                            if (stats!.GetValue(measurement, out var value))
                                            {
                                                yield return KeyValuePair.Create(measurement, value);
                                            }
                                        }
                                    }

                                    IEnumerable<KeyValuePair<string, string>> headerEnumerable()
                                    {
                                        foreach (var record in image.Header)
                                        {
                                            yield return KeyValuePair.Create(record.Key, record.Value.Value);
                                        }
                                    }

                                    cache.Save(file, cachedStatsEnumerable(), groupMatch != null ? groupMatch.Matches : new List<KeyValuePair<string, string?>>(), groupKey, headerEnumerable());

                                    eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.CacheFitEnd, file, index));
                                }
                                catch (Exception ex)
                                {
                                    eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.CacheFitEnd, file, index, ex));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.AnalyzeFitEnd, file, index, ex));
                        }
                        finally
                        {
                            image.Dispose();
                        }
                    }
                    finally
                    {
                        image.Dispose();
                    }
                }
                else
                {
                    eventConsumer?.Invoke(new BatchEvaluation.LoadEvent(BatchEvaluation.Phase.LoadFitEnd, file, index, false, false, new Exception("Failed loading image")));
                }
            }
            catch (Exception ex)
            {
                eventConsumer?.Invoke(new BatchEvaluation.LoadEvent(BatchEvaluation.Phase.LoadFitEnd, file, index, false, false, ex));
            }

            return stats != null ? Tuple.Create(stats, groupKey) : null;
        }

        private IEvaluationService.IEvaluator? BuildEvaluator(string formula, out string? errorMessage, IBatchEvaluationService.EventConsumer? eventConsumer = null)
        {
            eventConsumer?.Invoke(new BatchEvaluation.Event(BatchEvaluation.Phase.InitStart));

            IEvaluationService.IEvaluator? evaluator = null;

            try
            {
                if (!evaluationService.Build(formula, out evaluator, out errorMessage) || evaluator == null)
                {
                    if (errorMessage != null)
                    {
                        errorMessage = "Failed building evaluator due to an invalid evaluation formula: " + errorMessage;
                    }
                    else
                    {
                        errorMessage = "Failed building evaluator";
                    }
                    eventConsumer?.Invoke(new BatchEvaluation.Event(BatchEvaluation.Phase.InitEnd, new IBatchEvaluationService.InvalidConfigException(errorMessage, null)));
                }
                else
                {
                    eventConsumer?.Invoke(new BatchEvaluation.Event(BatchEvaluation.Phase.InitEnd));
                }
            }
            catch (Exception ex)
            {
                eventConsumer?.Invoke(new BatchEvaluation.Event(BatchEvaluation.Phase.InitEnd, ex));
                throw;
            }

            return evaluator;
        }

        public async Task EvaluateAsync(IReadOnlyJobConfig jobConfig, List<string> files, IBatchEvaluationService.EvaluationConsumer evaluationConsumer, IBatchEvaluationService.ICache? cache = null, IBatchEvaluationService.EventConsumer? eventConsumer = null, CancellationToken cancellationToken = default)
        {
            if (jobConfig.ParallelTasks <= 0)
            {
                throw new IBatchEvaluationService.InvalidConfigException(nameof(jobConfig.ParallelTasks) + " must be at least 1", null);
            }

            if (jobConfig.ParallelIO <= 0)
            {
                throw new IBatchEvaluationService.InvalidConfigException(nameof(jobConfig.ParallelIO) + " must be at least 1", null);
            }

            Filters? filters = null;
            if (jobConfig.GroupingFilters != null)
            {
                Dictionary<string, Regex> filterPatterns = new();

                foreach (var filter in jobConfig.GroupingFilters)
                {
                    try
                    {
                        Regex regex = new(filter.Pattern);
                        filterPatterns.TryAdd(filter.Key, regex);
                    }
                    catch (Exception ex)
                    {
                        throw new IBatchEvaluationService.InvalidConfigException("Invalid grouping filter pattern '" + filter.Pattern + "' in " + nameof(jobConfig.GroupingFilters), ex);
                    }
                }

                filters = new Filters(groupingManager.BuildGrouping(filterPatterns.Keys.ToArray()), filterPatterns);
            }

            var grouping = groupingManager.BuildGrouping(jobConfig.GroupingKeys != null ? jobConfig.GroupingKeys.ToArray() : Array.Empty<string>());

            var evaluator = BuildEvaluator(jobConfig.EvaluationFormula, out var errorMessage, eventConsumer);

            if (evaluator == null)
            {
                throw new IBatchEvaluationService.InvalidConfigException(errorMessage, null);
            }

            ConcurrentDictionary<string, Tuple<IFitsImageStatistics, int>> fileToStatistics = new();
            ConcurrentDictionary<IFitsImageStatistics, Tuple<string, int, string>> statisticsToFile = new();

            ConcurrentDictionary<string, List<string>> groups = new();

            var ioThrottle = new AsyncSemaphore(jobConfig.ParallelIO);

            // Load and group all statistics
            IEnumerable<Func<object, Func<CancellationToken, Task>>> loadTaskGenerator()
            {
                int index = 0;
                foreach (var file in files)
                {
                    yield return o => async ct =>
                    {
                        var tuple = await Task.Run(() => LoadAndCalculateStatisticsAsync(file, jobConfig, filters, ioThrottle, index, grouping, cache, eventConsumer, ct));
                        if (tuple != null)
                        {
                            var stats = tuple.Item1;
                            var groupKey = tuple.Item2 ?? "All";

                            fileToStatistics.TryAdd(file, Tuple.Create(stats, index));
                            statisticsToFile.TryAdd(stats, Tuple.Create(file, index, groupKey));

                            var groupFiles = groups.GetOrAdd(groupKey, _ => new());
                            lock (groupFiles)
                            {
                                groupFiles.Add(file);
                            }
                        }
                    };

                    ++index;
                }
            }
            await PooledResourceTaskRunner.RunAsync(new object[jobConfig.ParallelTasks], loadTaskGenerator(), cancellationToken);

            // Evaluate statistics for each group
            int i = 0;
            int n = groups.Count;
            foreach (var entry in groups)
            {
                var groupKey = entry.Key;
                var groupFiles = entry.Value!;

                IEvaluationService.IEvaluator.EventConsumer? internalEventConsumer = null;
                if (eventConsumer != null)
                {
                    int groupSize = groupFiles.Count;
                    internalEventConsumer = e =>
                    {
                        if (e is Evaluation.StatisticsEvent se)
                        {
                            if (e.Phase == Evaluation.Phase.PrecomputeStatisticsStart)
                            {
                                eventConsumer?.Invoke(new BatchEvaluation.StatisticsEvent(BatchEvaluation.Phase.PrecomputeStatisticsStart, se.Name, i, n, groupKey, groupSize));
                            }
                            else if (e.Phase == Evaluation.Phase.PrecomputeStatisticsEnd)
                            {
                                eventConsumer?.Invoke(new BatchEvaluation.StatisticsEvent(BatchEvaluation.Phase.PrecomputeStatisticsEnd, se.Name, i, n, groupKey, groupSize));
                            }
                        }
                        else if (e is Evaluation.EvaluationStepEvent ee)
                        {
                            if (e.Phase == Evaluation.Phase.EvaluationStepStart)
                            {
                                if (ee.StepIndex == 0)
                                {
                                    eventConsumer?.Invoke(new BatchEvaluation.EvaluationEvent(BatchEvaluation.Phase.EvaluationStart, statisticsToFile[ee.Stats].Item1, statisticsToFile[ee.Stats].Item2, i, n, groupKey, groupSize));
                                }

                                eventConsumer?.Invoke(new BatchEvaluation.EvaluationStepEvent(BatchEvaluation.Phase.EvaluationStepStart, statisticsToFile[ee.Stats].Item1, statisticsToFile[ee.Stats].Item2, i, n, groupKey, groupSize, ee.StepIndex, ee.StepCount));
                            }

                            if (e.Phase == Evaluation.Phase.EvaluationStepEnd)
                            {
                                eventConsumer?.Invoke(new BatchEvaluation.EvaluationStepEvent(BatchEvaluation.Phase.EvaluationStepEnd, statisticsToFile[ee.Stats].Item1, statisticsToFile[ee.Stats].Item2, i, n, groupKey, groupSize, ee.StepIndex, ee.StepCount));

                                if (ee.StepIndex == ee.StepCount - 1)
                                {
                                    eventConsumer?.Invoke(new BatchEvaluation.EvaluationEvent(BatchEvaluation.Phase.EvaluationEnd, statisticsToFile[ee.Stats].Item1, statisticsToFile[ee.Stats].Item2, i, n, groupKey, groupSize));
                                }
                            }
                        }
                    };
                }

                IEnumerable<IFitsImageStatistics> evaluateTaskGenerator()
                {
                    foreach (var file in groupFiles)
                    {
                        yield return fileToStatistics[file].Item1;
                    }
                }

                await evaluator.EvaluateAsync(evaluateTaskGenerator(), jobConfig.ParallelTasks, async (stats, variableValues, value, ct) =>
                {
                    var tuple = statisticsToFile[stats];
                    try
                    {
                        eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.ConsumeEvaluationStart, tuple.Item1, tuple.Item2));

                        await evaluationConsumer.Invoke(tuple.Item1, tuple.Item3, variableValues, value, ct);

                        eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.ConsumeEvaluationEnd, tuple.Item1, tuple.Item2));
                    }
                    catch (Exception ex)
                    {
                        eventConsumer?.Invoke(new BatchEvaluation.FileEvent(BatchEvaluation.Phase.ConsumeEvaluationEnd, tuple.Item1, tuple.Item2, ex));
                    }
                }, internalEventConsumer, cancellationToken);

                ++i;
            }
        }
    }
}
