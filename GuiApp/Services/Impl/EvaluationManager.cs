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
using FitsRatingTool.GuiApp.Models;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.FitsImage;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections.Generic;
using ReactiveUI;
using System.Reactive.Concurrency;
using Avalonia.Utilities;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class EvaluationManager : IEvaluationManager
    {
        private IEvaluationService.IEvaluator? cachedEvaluator;
        private string? cachedEvaluatorFormula;


        private readonly IEvaluationService evaluationService;
        private readonly IFitsImageManager manager;

        public EvaluationManager(IEvaluationService evaluationService, IFitsImageManager manager)
        {
            this.evaluationService = evaluationService;
            this.manager = manager;

            CurrentFormulaChanged += (s, e) =>
            {
                if (AutoUpdateRatings)
                {
                    ScheduleUpdateRatings();
                }
            };

            CurrentGroupingChanged += (s, e) =>
            {
                if (AutoUpdateRatings)
                {
                    ScheduleUpdateRatings();
                }
            };

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, EvaluationManager>(manager, nameof(manager.RecordChanged), OnRecordChanged);
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (AutoUpdateRatings &&
                (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.File && args.Removed
                || args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Statistics
                || args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Metadata))
            {
                var metadata = manager.Get(args.File)?.Metadata;
                var stats = manager.Get(args.File)?.Statistics;

                // Updating ratings only makes sense if both the metadata and stats are available
                if (metadata != null && stats != null)
                {
                    var currentGrouping = CurrentGrouping;

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
                else if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.File)
                {
                    // Or when the file is removed
                    ScheduleUpdateRatings();
                }
            }
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
            var evaluatorInstance = evaluator;

            if (evaluatorInstance == null)
            {
                if (cachedEvaluatorFormula != null && cachedEvaluatorFormula.Equals(CurrentFormula))
                {
                    evaluatorInstance = cachedEvaluator;
                }
                else
                {
                    cachedEvaluator = null;
                    cachedEvaluatorFormula = CurrentFormula;

                    if (cachedEvaluatorFormula != null && evaluationService.Build(cachedEvaluatorFormula, out var newEvaluatorInstance, out var _) && newEvaluatorInstance != null)
                    {
                        evaluatorInstance = cachedEvaluator = newEvaluatorInstance;
                    }
                }
            }

            if (evaluatorInstance != null)
            {
                var currentGrouping = CurrentGrouping;

                Dictionary<string, List<IFitsImageStatisticsViewModel>> groups = new();

                foreach (var file in manager.Files)
                {
                    var record = manager.Get(file);

                    if (record != null)
                    {
                        var stats = record.Statistics;

                        if (stats != null)
                        {
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
                                }
                            }
                            groupKey ??= "All";

                            if (groupKey != null && (specificGroupKey == null || specificGroupKey.Equals(groupKey)))
                            {
                                if (!groups.TryGetValue(groupKey, out var statistics))
                                {
                                    groups.Add(groupKey, statistics = new());
                                }

                                statistics.Add(stats);
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

        private async Task UpdateRatingsForGroupAsync(IEvaluationService.IEvaluator evaluatorInstance, List<IFitsImageStatisticsViewModel> statistics)
        {
            try
            {
                ConcurrentDictionary<IFitsImageStatisticsViewModel, double> results = new();

                await evaluatorInstance.EvaluateAsync(statistics, 16, (stats, variableValues, result, ct) =>
                {
                    results.TryAdd((IFitsImageStatisticsViewModel)stats, result);
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



        private IGroupingManager.IGrouping? _currentGrouping;
        public IGroupingManager.IGrouping? CurrentGrouping
        {
            get => _currentGrouping;
            set
            {
                if (_currentGrouping != value)
                {
                    var old = _currentGrouping;
                    _currentGrouping = value;
                    _currentGroupingChanged?.Invoke(this, new IEvaluationManager.GroupingChangedEventArgs(old, value));
                }
            }
        }

        private IGroupingManager.IGrouping? _currentFilterGrouping;
        public IGroupingManager.IGrouping? CurrentFilterGrouping
        {
            get => _currentFilterGrouping;
            set
            {
                if (_currentFilterGrouping != value)
                {
                    var old = _currentFilterGrouping;
                    _currentFilterGrouping = value;
                    _currentFilterGroupingChanged?.Invoke(this, new IEvaluationManager.GroupingChangedEventArgs(old, value));
                }
            }
        }

        private string? _currentFilterGroupKey;
        public string? CurrentFilterGroupKey
        {
            get => _currentFilterGroupKey;
            set
            {
                if (!string.Equals(_currentFilterGroupKey, value))
                {
                    var old = _currentFilterGroupKey;
                    _currentFilterGroupKey = value;
                    _currentFilterGroupKeyChanged?.Invoke(this, new IEvaluationManager.GroupKeyChangedEventArgs(old, value));
                }
            }
        }

        private string? _currentFormula;
        public string? CurrentFormula
        {
            get => _currentFormula;
            set
            {
                if (!string.Equals(_currentFormula, value))
                {
                    var old = _currentFormula;
                    _currentFormula = value;
                    _currentFormulaChanged?.Invoke(this, new IEvaluationManager.FormulaChangedEventArgs(old, value));
                }
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

        private GroupingConfiguration? _currentGroupingConfiguration;
        public GroupingConfiguration? CurrentGroupingConfiguration
        {
            get => _currentGroupingConfiguration;
            set
            {
                if (!string.Equals(_currentGroupingConfiguration, value))
                {
                    var old = _currentGroupingConfiguration;
                    _currentGroupingConfiguration = value;
                    _currentGroupingConfigurationChanged?.Invoke(this, new IEvaluationManager.GroupingConfigurationChangedEventArgs(old, value));
                }
            }
        }

        private GroupingConfiguration? _currentFilterGroupingConfiguration;
        public GroupingConfiguration? CurrentFilterGroupingConfiguration
        {
            get => _currentFilterGroupingConfiguration;
            set
            {
                if (!string.Equals(_currentFilterGroupingConfiguration, value))
                {
                    var old = _currentFilterGroupingConfiguration;
                    _currentFilterGroupingConfiguration = value;
                    _currentFilterGroupingConfigurationChanged?.Invoke(this, new IEvaluationManager.GroupingConfigurationChangedEventArgs(old, value));
                }
            }
        }



        private event EventHandler<IEvaluationManager.GroupingChangedEventArgs>? _currentGroupingChanged;
        public event EventHandler<IEvaluationManager.GroupingChangedEventArgs> CurrentGroupingChanged
        {
            add
            {
                _currentGroupingChanged += value;
            }
            remove
            {
                _currentGroupingChanged -= value;
            }
        }

        private event EventHandler<IEvaluationManager.GroupingChangedEventArgs>? _currentFilterGroupingChanged;
        public event EventHandler<IEvaluationManager.GroupingChangedEventArgs> CurrentFilterGroupingChanged
        {
            add
            {
                _currentFilterGroupingChanged += value;
            }
            remove
            {
                _currentFilterGroupingChanged -= value;
            }
        }

        private event EventHandler<IEvaluationManager.GroupKeyChangedEventArgs>? _currentFilterGroupKeyChanged;
        public event EventHandler<IEvaluationManager.GroupKeyChangedEventArgs> CurrentFilterGroupKeyChanged
        {
            add
            {
                _currentFilterGroupKeyChanged += value;
            }
            remove
            {
                _currentFilterGroupKeyChanged -= value;
            }
        }

        private event EventHandler<IEvaluationManager.GroupingConfigurationChangedEventArgs>? _currentGroupingConfigurationChanged;
        public event EventHandler<IEvaluationManager.GroupingConfigurationChangedEventArgs> CurrentGroupingConfigurationChanged
        {
            add
            {
                _currentGroupingConfigurationChanged += value;
            }
            remove
            {
                _currentGroupingConfigurationChanged -= value;
            }
        }

        private event EventHandler<IEvaluationManager.GroupingConfigurationChangedEventArgs>? _currentFilterGroupingConfigurationChanged;
        public event EventHandler<IEvaluationManager.GroupingConfigurationChangedEventArgs> CurrentFilterGroupingConfigurationChanged
        {
            add
            {
                _currentFilterGroupingConfigurationChanged += value;
            }
            remove
            {
                _currentFilterGroupingConfigurationChanged -= value;
            }
        }

        private event EventHandler<IEvaluationManager.FormulaChangedEventArgs>? _currentFormulaChanged;
        public event EventHandler<IEvaluationManager.FormulaChangedEventArgs> CurrentFormulaChanged
        {
            add
            {
                _currentFormulaChanged += value;
            }
            remove
            {
                _currentFormulaChanged -= value;
            }
        }
    }
}
