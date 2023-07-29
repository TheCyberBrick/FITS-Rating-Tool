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

using FitsRatingTool.Common.Services;
using System;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.FitsImage;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections.Generic;
using ReactiveUI;
using System.Reactive.Concurrency;
using Avalonia.Utilities;
using DryIocAttributes;
using System.ComponentModel.Composition;
using FitsRatingTool.Common.Models.Evaluation;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    [Export(typeof(IEvaluationManager)), SingletonReuse]
    public class EvaluationManager : IEvaluationManager
    {
        private IEvaluationContext? _evaluationContext;
        public IEvaluationContext? EvaluationContext
        {
            get => _evaluationContext;
            set
            {
                if (_evaluationContext != value)
                {
                    _evaluationContext = value;
                    InvalidateEvaluation();
                }
            }
        }

        private IVariableContext? _variableContext;
        public IVariableContext? VariableContext
        {
            get => _variableContext;
            set
            {
                if (_variableContext != value)
                {
                    _variableContext = value;
                    InvalidateEvaluation();
                }
            }
        }

        private readonly IEvaluationService evaluationService;
        private readonly IFitsImageManager manager;

        public EvaluationManager(IEvaluationService evaluationService, IFitsImageManager manager)
        {
            this.evaluationService = evaluationService;
            this.manager = manager;

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, EvaluationManager>(manager, nameof(manager.RecordChanged), OnRecordChanged);
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (AutoUpdateRatings &&
                (args.Type == IFitsImageManager.RecordChangedEventArgs.ChangeType.File && args.Removed
                || args.Type == IFitsImageManager.RecordChangedEventArgs.ChangeType.Statistics
                || args.Type == IFitsImageManager.RecordChangedEventArgs.ChangeType.Metadata))
            {
                var metadata = manager.Get(args.File)?.Metadata;
                var stats = manager.Get(args.File)?.Statistics;

                // Updating ratings only makes sense if both the metadata and stats are available
                if (metadata != null && stats != null)
                {
                    var currentGrouping = EvaluationContext?.CurrentGrouping;

                    // Try to find group key for the changed image
                    // so we can only update the ratings of images
                    // in the same group
                    string? groupKey = null;
                    if (currentGrouping != null && !currentGrouping.IsAll)
                    {
                        var match = currentGrouping.GetGroupMatch(metadata);
                        if (match != null)
                        {
                            groupKey = match.GroupKey;
                        }
                    }
                    groupKey ??= "All";

                    ScheduleUpdateRatings(groupKey);
                }
                else if (args.Type == IFitsImageManager.RecordChangedEventArgs.ChangeType.File)
                {
                    // Or when the file is removed
                    ScheduleUpdateRatings();
                }
            }
        }

        public void InvalidateEvaluation()
        {
            if (AutoUpdateRatings)
            {
                ScheduleUpdateRatings();
            }
        }

        public void InvalidateStatistics()
        {
            foreach (var file in manager.Files)
            {
                var record = manager.Get(file);

                if (record != null)
                {
                    record.IsOutdated = true;
                }
            }

            InvalidateEvaluation();
        }

        private void ScheduleUpdateRatings(string? specificGroupKey = null)
        {
            async void update(string? specificGroupKey = null)
            {
                await UpdateRatingsAsync(specificGroupKey);
            }

            RxApp.MainThreadScheduler.Schedule(() => update(specificGroupKey));
        }

        public async Task UpdateRatingsAsync(string? specificGroupKey = null, IEvaluationService.IEvaluator? evaluator = null)
        {
            var variables = VariableContext?.CurrentVariables;

            var evaluatorInstance = EvaluationContext?.CurrentEvaluator;

            if (evaluatorInstance != null)
            {
                var currentGrouping = EvaluationContext?.CurrentGrouping;

                Dictionary<string, List<EvaluationItem>> groups = new();

                foreach (var file in manager.Files)
                {
                    var record = manager.Get(file);

                    if (record != null)
                    {
                        var stats = record.Statistics;

                        if (stats != null)
                        {
                            IDictionary<string, Constant>? constants = null;

                            string? groupKey = null;
                            if (currentGrouping != null && !currentGrouping.IsAll)
                            {
                                var metadata = record.Metadata;

                                if (metadata != null)
                                {
                                    var match = currentGrouping.GetGroupMatch(metadata);
                                    if (match != null)
                                    {
                                        groupKey = match.GroupKey;
                                    }

                                    if (variables != null)
                                    {
                                        Func<string, string?> headerMap = keyword =>
                                        {
                                            foreach (var record in metadata.Header)
                                            {
                                                if (record.Keyword == keyword)
                                                {
                                                    return record.Value;
                                                }
                                            }
                                            return null;
                                        };

                                        constants = await evaluationService.EvaluateVariablesAsync(variables, file, headerMap);
                                    }
                                }
                            }
                            groupKey ??= "All";

                            if (groupKey != null && (specificGroupKey == null || specificGroupKey.Equals(groupKey)))
                            {
                                if (!groups.TryGetValue(groupKey, out var statistics))
                                {
                                    groups.Add(groupKey, statistics = new());
                                }

                                statistics.Add(new EvaluationItem(stats, constants));
                            }
                        }
                    }
                }

                foreach (var statistics in groups.Values)
                {
                    await UpdateRatingsForGroupAsync(evaluatorInstance, statistics);
                }
            }
        }

        private async Task UpdateRatingsForGroupAsync(IEvaluationService.IEvaluator evaluatorInstance, List<EvaluationItem> items)
        {
            try
            {
                ConcurrentDictionary<IFitsImageStatisticsViewModel, double> results = new();

                await evaluatorInstance.EvaluateAsync(items, 16, (item, variableValues, result, ct) =>
                {
                    results.TryAdd((IFitsImageStatisticsViewModel)item.Statistics, result);
                    return Task.CompletedTask;
                });

                foreach (var entry in results)
                {
                    entry.Key.Rating = entry.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private bool _autoUpdateRatings = true;
        public bool AutoUpdateRatings
        {
            get => _autoUpdateRatings;
            set
            {
                bool old = _autoUpdateRatings;
                _autoUpdateRatings = value;
                if (value && !old)
                {
                    ScheduleUpdateRatings();
                }
            }
        }
    }
}
