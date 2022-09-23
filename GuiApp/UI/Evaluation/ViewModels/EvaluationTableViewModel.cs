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

using Avalonia.Collections;
using Avalonia.Utilities;
using ReactiveUI;
using System.Collections.Generic;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.FitsImage;
using System;
using System.Reactive.Linq;
using System.Reactive;
using System.Threading.Tasks;
using FitsRatingTool.Common.Services;
using FitsRatingTool.GuiApp.Models;
using System.Linq;
using FitsRatingTool.Common.Models.FitsImage;
using Microsoft.VisualStudio.Threading;
using System.Reactive.Concurrency;
using System.Collections;

namespace FitsRatingTool.GuiApp.UI.Evaluation.ViewModels
{
    public class EvaluationTableViewModel : ViewModelBase, IEvaluationTableViewModel
    {
        public class Factory : IEvaluationTableViewModel.IFactory
        {
            private readonly IFitsImageManager manager;
            private readonly IGroupingManager groupingManager;
            private readonly IEvaluationManager evaluationManager;
            private readonly IJobGroupingConfiguratorViewModel.IFactory groupingConfiguratorFactory;
            private readonly IEvaluationExporterViewModel.IFactory evaluationExporterFactory;

            public Factory(IFitsImageManager manager, IGroupingManager groupingManager, IEvaluationManager evaluationManager,
                IJobGroupingConfiguratorViewModel.IFactory groupingConfiguratorFactory, IEvaluationExporterViewModel.IFactory evaluationExporterFactory)
            {
                this.manager = manager;
                this.groupingManager = groupingManager;
                this.evaluationManager = evaluationManager;
                this.groupingConfiguratorFactory = groupingConfiguratorFactory;
                this.evaluationExporterFactory = evaluationExporterFactory;
            }

            public IEvaluationTableViewModel Create()
            {
                return new EvaluationTableViewModel(manager, groupingManager, evaluationManager, groupingConfiguratorFactory, evaluationExporterFactory);
            }
        }


        private class Record : IEvaluationTableViewModel.Record
        {
            public Record(long id, string file, IFitsImageStatisticsViewModel? stats, EvaluationTableViewModel vm) : base(id, file, stats)
            {
                this.WhenAnyValue(x => x.Statistics!.Rating)
                    .Skip(1)
                    .Subscribe(x => vm.ScheduleGraphsUpdate());
            }
        }

        private readonly Dictionary<string, IEvaluationTableViewModel.Record> recordMap = new();

        private AvaloniaList<IEvaluationTableViewModel.Record> _records = new();
        public AvaloniaList<IEvaluationTableViewModel.Record> Records
        {
            get => _records;
            set => this.RaiseAndSetIfChanged(ref _records, value);
        }

        private IEvaluationTableViewModel.Record? _selectedRecord;
        public IEvaluationTableViewModel.Record? SelectedRecord
        {
            get => _selectedRecord;
            set => this.RaiseAndSetIfChanged(ref _selectedRecord, value);
        }

        public ReactiveCommand<IEnumerable, Unit> RemoveRecords { get; }


        private int _dataKeyIndex = 0;
        public int DataKeyIndex
        {
            get => _dataKeyIndex;
            set => this.RaiseAndSetIfChanged(ref _dataKeyIndex, value);
        }

        private double _graphRangeInMADSigmas = 2.0;
        public double GraphRangeInMADSigmas
        {
            get => _graphRangeInMADSigmas;
            set => this.RaiseAndSetIfChanged(ref _graphRangeInMADSigmas, value);
        }

        private double _graphMin = 0;
        public double GraphMin
        {
            get => _graphMin;
            set => this.RaiseAndSetIfChanged(ref _graphMin, value);
        }

        private double _graphMax = 1;
        public double GraphMax
        {
            get => _graphMax;
            set => this.RaiseAndSetIfChanged(ref _graphMax, value);
        }

        private double _ratingGraphMin = 0;
        public double RatingGraphMin
        {
            get => _ratingGraphMin;
            set => this.RaiseAndSetIfChanged(ref _ratingGraphMin, value);
        }

        private double _ratingGraphMax = 1;
        public double RatingGraphMax
        {
            get => _ratingGraphMax;
            set => this.RaiseAndSetIfChanged(ref _ratingGraphMax, value);
        }

        private IEvaluationTableViewModel.SigmaRange _zeroSigmaRange;
        public IEvaluationTableViewModel.SigmaRange ZeroSigmaRange
        {
            get => _zeroSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _zeroSigmaRange, value);
        }

        private IEvaluationTableViewModel.SigmaRange _halfSigmaRange;
        public IEvaluationTableViewModel.SigmaRange HalfSigmaRange
        {
            get => _halfSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _halfSigmaRange, value);
        }

        private IEvaluationTableViewModel.SigmaRange _oneSigmaRange;
        public IEvaluationTableViewModel.SigmaRange OneSigmaRange
        {
            get => _oneSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _oneSigmaRange, value);
        }

        private IEvaluationTableViewModel.SigmaRange _twoSigmaRange;
        public IEvaluationTableViewModel.SigmaRange TwoSigmaRange
        {
            get => _twoSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _twoSigmaRange, value);
        }

        private IEvaluationTableViewModel.SigmaRange _threeSigmaRange;
        public IEvaluationTableViewModel.SigmaRange ThreeSigmaRange
        {
            get => _threeSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _threeSigmaRange, value);
        }

        private IEvaluationTableViewModel.SigmaRange _ratingZeroSigmaRange;
        public IEvaluationTableViewModel.SigmaRange RatingZeroSigmaRange
        {
            get => _ratingZeroSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _ratingZeroSigmaRange, value);
        }

        private IEvaluationTableViewModel.SigmaRange _ratingHalfSigmaRange;
        public IEvaluationTableViewModel.SigmaRange RatingHalfSigmaRange
        {
            get => _ratingHalfSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _ratingHalfSigmaRange, value);
        }

        private IEvaluationTableViewModel.SigmaRange _ratingOneSigmaRange;
        public IEvaluationTableViewModel.SigmaRange RatingOneSigmaRange
        {
            get => _ratingOneSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _ratingOneSigmaRange, value);
        }

        private IEvaluationTableViewModel.SigmaRange _ratingTwoSigmaRange;
        public IEvaluationTableViewModel.SigmaRange RatingTwoSigmaRange
        {
            get => _ratingTwoSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _ratingTwoSigmaRange, value);
        }

        private IEvaluationTableViewModel.SigmaRange _ratingThreeSigmaRange;
        public IEvaluationTableViewModel.SigmaRange RatingThreeSigmaRange
        {
            get => _ratingThreeSigmaRange;
            set => this.RaiseAndSetIfChanged(ref _ratingThreeSigmaRange, value);
        }

        public AvaloniaList<string> GroupKeys { get; } = new() { "All" };

        private string _selectedGroupKey = "All";
        public string SelectedGroupKey
        {
            get => _selectedGroupKey;
            set => this.RaiseAndSetIfChanged(ref _selectedGroupKey, value);
        }

        public IJobGroupingConfiguratorViewModel GroupingConfigurator { get; }


        public ReactiveCommand<Unit, IEvaluationExporterViewModel> ShowEvaluationExporter { get; }



        private readonly AsyncSemaphore updateGraphsSemaphore = new(1);

        private bool graphsDirty = false;


        private readonly IFitsImageManager manager;
        private readonly IGroupingManager groupingManager;
        private readonly IEvaluationManager evaluationManager;


        public EvaluationTableViewModel(IFitsImageManager manager, IGroupingManager groupingManager, IEvaluationManager evaluationManager,
            IJobGroupingConfiguratorViewModel.IFactory groupingConfiguratorFactory, IEvaluationExporterViewModel.IFactory evaluationExporterFactory)
        {
            this.manager = manager;
            this.groupingManager = groupingManager;
            this.evaluationManager = evaluationManager;

            var defaultGroupingConfiguration = evaluationManager.CurrentFilterGroupingConfiguration;
            if (defaultGroupingConfiguration == null)
            {
                // By default group by object
                defaultGroupingConfiguration = new GroupingConfiguration(true, false, false, false, false, false, 0, null);
            }

            GroupingConfigurator = groupingConfiguratorFactory.Create(defaultGroupingConfiguration);
            evaluationManager.CurrentFilterGroupingConfiguration = GroupingConfigurator.GroupingConfiguration;

            var currentGroupKey = evaluationManager.CurrentFilterGroupKey;

            using (DelayChangeNotifications())
            {
                foreach (var file in manager.Files)
                {
                    var record = manager.Get(file);
                    if (record != null)
                    {
                        AddRecord(record.Id, file);
                    }
                }
            }

            this.WhenAnyValue(x => x.SelectedRecord).Skip(1).Subscribe(x =>
            {
                foreach (var record in recordMap.Values)
                {
                    record.IsSelected = false;
                }
                if (x != null)
                {
                    x.IsSelected = true;
                }
            });

            this.WhenAnyValue(x => x.DataKeyIndex).Skip(1).Subscribe(x => UpdateGraphsImmediately());

            this.WhenAnyValue(x => x.GraphRangeInMADSigmas).Skip(1).Subscribe(x => UpdateGraphsImmediately());

            this.WhenAnyValue(x => x.SelectedGroupKey).Skip(1).Subscribe(x =>
            {
                evaluationManager.CurrentFilterGroupKey = x;
                UpdateRecords();
                UpdateGraphsImmediately();
            });

            this.WhenAnyValue(x => x.GroupingConfigurator.GroupingConfiguration).Skip(1).Subscribe(x => UpdateGroupingConfiguration(x));

            foreach (var groupKey in GroupKeys)
            {
                if (groupKey.Equals(currentGroupKey))
                {
                    SelectedGroupKey = currentGroupKey;
                    UpdateGroupsAndRecords();
                    break;
                }
            }

            UpdateGraphsImmediately();

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, EvaluationTableViewModel>(manager, nameof(manager.RecordChanged), OnRecordChanged);


            ShowEvaluationExporter = ReactiveCommand.Create(() => evaluationExporterFactory.Create());

            RemoveRecords = ReactiveCommand.CreateFromTask<IEnumerable>(async enumerable =>
            {
                var toRemove = new List<IEvaluationTableViewModel.Record>();
                foreach (var obj in enumerable)
                {
                    if (obj is IEvaluationTableViewModel.Record record)
                    {
                        toRemove.Add(record);
                    }
                }
                foreach (var record in toRemove)
                {
                    await record.Remove.Execute(Unit.Default);
                }
            });
        }

        private void AddRecord(long id, string file)
        {
            if (!recordMap.TryGetValue(file, out var record))
            {
                record = new Record(id, file, manager.Get(file)?.Statistics, this);

                record.Remove.Subscribe(_ => manager.Remove(file));

                recordMap.Add(file, record);
            }

            UpdateRecordState(record, true);
        }

        private void UpdateRecordState(string file)
        {
            if (recordMap.TryGetValue(file, out var record))
            {
                UpdateRecordState(record, false);
            }
        }

        private void UpdateRecordState(IEvaluationTableViewModel.Record record, bool initialAdd)
        {
            bool changed = false;

            var imageRecord = manager.Get(record.File);

            if (imageRecord != null)
            {
                record.IsOutdated = imageRecord.IsOutdated;
            }

            var stats = imageRecord?.Statistics;

            if (record.Statistics != stats)
            {
                changed = true;
                record.Statistics = stats;
            }

            UpdateRecordGroup(evaluationManager.CurrentFilterGrouping, record, manager.Get(record.File)?.Metadata);

            if (IsRecordShown(record))
            {
                if (initialAdd || record.IsFilteredOut)
                {
                    changed = true;
                    record.IsFilteredOut = false;
                    record.Index = Records.Count;
                    Records.Add(record);
                }
            }
            else
            {
                if (!record.IsFilteredOut)
                {
                    changed = true;
                    record.IsFilteredOut = true;
                    Records.Remove(record);
                }
            }

            if (changed)
            {
                ScheduleGraphsUpdate();
            }
        }

        private bool RemoveRecord(string file, out IEvaluationTableViewModel.Record? record)
        {
            if (SelectedRecord != null && file.Equals(SelectedRecord.File))
            {
                SelectedRecord.IsSelected = false;
                SelectedRecord = null;
            }

            if (recordMap.Remove(file, out record) && record != null)
            {
                Records.Remove(record);

                ResetIndices();

                ScheduleGraphsUpdate();

                return true;
            }

            return false;
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.File)
            {
                if (args.AddedOrUpdated)
                {
                    var record = manager.Get(args.File);
                    if (record != null)
                    {
                        AddRecord(record.Id, args.File);
                    }
                }
                else
                {
                    RemoveRecord(args.File, out _);
                }
            }
            else if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Statistics)
            {
                UpdateRecordState(args.File);
            }
            else if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Metadata && args.AddedOrUpdated)
            {
                UpdateRecordState(args.File);
            }
            else if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Outdated)
            {
                UpdateRecordState(args.File);
            }
        }

        private void ResetIndices()
        {
            using (DelayChangeNotifications())
            {
                int i = 0;
                foreach (var record in Records)
                {
                    record.Index = i;
                    ++i;
                }
            }
        }

        private static bool GetValueByMeasurement(IEvaluationTableViewModel.Record? record, IFitsImageStatisticsViewModel.MeasurementType? measurement, out double value)
        {
            value = 0;

            if (record == null || record.Statistics == null)
            {
                return false;
            }

            return record.Statistics.GetValue(measurement, out value);
        }

        private IFitsImageStatisticsViewModel.MeasurementType? GetSelectedMeasurementType()
        {
            return DataKeyIndex switch
            {
                0 => IFitsImageStatisticsViewModel.MeasurementType.HFDMean,
                1 => IFitsImageStatisticsViewModel.MeasurementType.FWHMMean,
                2 => IFitsImageStatisticsViewModel.MeasurementType.SNRMean,
                3 => IFitsImageStatisticsViewModel.MeasurementType.SNRWeight,
                4 => IFitsImageStatisticsViewModel.MeasurementType.EccentricityMean,
                5 => IFitsImageStatisticsViewModel.MeasurementType.Median,
                6 => IFitsImageStatisticsViewModel.MeasurementType.Noise,
                7 => IFitsImageStatisticsViewModel.MeasurementType.NoiseRatio,
                8 => IFitsImageStatisticsViewModel.MeasurementType.Stars,
                9 => IFitsImageStatisticsViewModel.MeasurementType.ResidualMean,
                10 => IFitsImageStatisticsViewModel.MeasurementType.HFDMAD,
                11 => IFitsImageStatisticsViewModel.MeasurementType.FWHMMAD,
                12 => IFitsImageStatisticsViewModel.MeasurementType.SNRMAD,
                13 => IFitsImageStatisticsViewModel.MeasurementType.EccentricityMAD,
                14 => IFitsImageStatisticsViewModel.MeasurementType.MedianMAD,
                15 => IFitsImageStatisticsViewModel.MeasurementType.ResidualMAD,
                _ => null,
            };
        }

        private void ScheduleGraphsUpdate()
        {
            graphsDirty = true;
            RxApp.MainThreadScheduler.Schedule(UpdateGraphs);
        }

        private async void UpdateGraphs()
        {
            // Using a semaphore here to prevent stale data causing
            // layout issues when multiple update tasks are running
            using (await updateGraphsSemaphore.EnterAsync())
            {
                if (graphsDirty)
                {
                    graphsDirty = false;
                    await UpdateMeasuresGraphAsync();
                    await UpdateRatingsGraphAsync();
                    //await Task.Delay(50);
                }
            }
        }

        private void UpdateGraphsImmediately()
        {
            UpdateMeasuresGraph();
            UpdateRatingsGraph();
        }

        private async Task UpdateMeasuresGraphAsync()
        {
            // Need to copy the list so the calculations can be
            // done safely off main thread
            var recordsCopy = new List<IEvaluationTableViewModel.Record>(Records);
            var measures = await Task.Run(() => CalculateMeasures(recordsCopy, GetSelectedMeasurementType()));
            recordsCopy = null;

            GraphMin = Math.Min(measures.min, measures.median - measures.mad * GraphRangeInMADSigmas);
            GraphMax = Math.Max(GraphMin + 0.001, Math.Max(measures.max, measures.median + measures.mad * GraphRangeInMADSigmas));

            var graphRange = GraphMax - GraphMin;

            var medianFromMin = measures.median - GraphMin;

            IEvaluationTableViewModel.SigmaRange createRange(double multiple)
            {
                return new IEvaluationTableViewModel.SigmaRange(measures.median - measures.mad * multiple, measures.median + measures.mad * multiple, Math.Max(medianFromMin - measures.mad * multiple, 0), Math.Max(graphRange - (medianFromMin + measures.mad * multiple), 0), measures.mad * multiple * 2);
            };

            ZeroSigmaRange = createRange(0.0);
            HalfSigmaRange = createRange(0.5);
            OneSigmaRange = createRange(1.0);
            TwoSigmaRange = createRange(2.0);
            ThreeSigmaRange = createRange(3.0);
        }

        private async Task UpdateRatingsGraphAsync()
        {
            // Need to copy the list so the calculations can be
            // done safely off main thread
            var recordsCopy = new List<IEvaluationTableViewModel.Record>(Records);
            var measures = await Task.Run(() => CalculateMeasures(recordsCopy, IFitsImageStatisticsViewModel.MeasurementType.Rating));
            recordsCopy = null;

            RatingGraphMin = Math.Min(measures.min, measures.median - measures.mad * GraphRangeInMADSigmas);
            RatingGraphMax = Math.Max(RatingGraphMin + 0.001, Math.Max(measures.max, measures.median + measures.mad * GraphRangeInMADSigmas));

            var graphRange = RatingGraphMax - RatingGraphMin;

            var medianFromMin = measures.median - RatingGraphMin;

            IEvaluationTableViewModel.SigmaRange createRange(double multiple)
            {
                return new IEvaluationTableViewModel.SigmaRange(measures.median - measures.mad * multiple, measures.median + measures.mad * multiple, Math.Max(medianFromMin - measures.mad * multiple, 0), Math.Max(graphRange - (medianFromMin + measures.mad * multiple), 0), measures.mad * multiple * 2);
            };

            RatingZeroSigmaRange = createRange(0.0);
            RatingHalfSigmaRange = createRange(0.5);
            RatingOneSigmaRange = createRange(1.0);
            RatingTwoSigmaRange = createRange(2.0);
            RatingThreeSigmaRange = createRange(3.0);
        }

        private void UpdateMeasuresGraph()
        {
            var measures = CalculateMeasures(Records, GetSelectedMeasurementType());

            GraphMin = Math.Min(measures.min, measures.median - measures.mad * GraphRangeInMADSigmas);
            GraphMax = Math.Max(GraphMin + 0.01, Math.Max(measures.max, measures.median + measures.mad * GraphRangeInMADSigmas));

            var graphRange = GraphMax - GraphMin;

            var medianFromMin = measures.median - GraphMin;

            IEvaluationTableViewModel.SigmaRange createRange(double multiple)
            {
                return new IEvaluationTableViewModel.SigmaRange(measures.median - measures.mad * multiple, measures.median + measures.mad * multiple, Math.Max(medianFromMin - measures.mad * multiple, 0), Math.Max(graphRange - (medianFromMin + measures.mad * multiple), 0), measures.mad * multiple * 2);
            };

            ZeroSigmaRange = createRange(0.0);
            HalfSigmaRange = createRange(0.5);
            OneSigmaRange = createRange(1.0);
            TwoSigmaRange = createRange(2.0);
            ThreeSigmaRange = createRange(3.0);
        }

        private void UpdateRatingsGraph()
        {
            var measures = CalculateMeasures(Records, IFitsImageStatisticsViewModel.MeasurementType.Rating);

            RatingGraphMin = Math.Min(measures.min, measures.median - measures.mad * GraphRangeInMADSigmas);
            RatingGraphMax = Math.Max(RatingGraphMin + 0.01, Math.Max(measures.max, measures.median + measures.mad * GraphRangeInMADSigmas));

            var graphRange = RatingGraphMax - RatingGraphMin;

            var medianFromMin = measures.median - RatingGraphMin;

            IEvaluationTableViewModel.SigmaRange createRange(double multiple)
            {
                return new IEvaluationTableViewModel.SigmaRange(measures.median - measures.mad * multiple, measures.median + measures.mad * multiple, Math.Max(medianFromMin - measures.mad * multiple, 0), Math.Max(graphRange - (medianFromMin + measures.mad * multiple), 0), measures.mad * multiple * 2);
            };

            RatingZeroSigmaRange = createRange(0.0);
            RatingHalfSigmaRange = createRange(0.5);
            RatingOneSigmaRange = createRange(1.0);
            RatingTwoSigmaRange = createRange(2.0);
            RatingThreeSigmaRange = createRange(3.0);
        }

        private struct Measures
        {
            public double min, max, sum, mean, median, mad, var, stddev;

            public double NormalizedDeviation(double value)
            {
                return (value - median) / mad;
            }
        }

        private static Measures CalculateMeasures(IList<IEvaluationTableViewModel.Record> records, IFitsImageStatisticsViewModel.MeasurementType? dataKey)
        {
            int i = 0;

            double min = double.MaxValue;
            double max = double.MinValue;
            double sum = 0;

            var values = new List<double>(records.Count);

            foreach (var record in records)
            {
                if (GetValueByMeasurement(record, dataKey, out var value))
                {
                    ++i;
                    sum += value;
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                    values.Add(value);
                }
            }

            if (i == 0)
            {
                return new Measures();
            }

            values.Sort();

            double median;
            if (i <= 1)
            {
                median = values[0];
            }
            else if (i % 2 == 0)
            {
                int half = i / 2 - 1;
                median = (values[half] + values[half + 1]) * 0.5;
            }
            else
            {
                median = values[(i - 1) / 2];
            }

            double mean = sum / i;
            double var = 0;
            double mad = 0;

            foreach (var record in records)
            {
                if (GetValueByMeasurement(record, dataKey, out var value))
                {
                    var += (mean - value) * (mean - value);
                    mad += Math.Abs(median - value);
                }
            }

            var /= i;
            mad /= i;

            return new Measures
            {
                min = min,
                max = max,
                sum = sum,
                mean = mean,
                median = median,
                mad = mad,
                var = var,
                stddev = Math.Sqrt(var)
            };
        }

        private void UpdateGroupsAndRecords(string? file = null)
        {
            using (DelayChangeNotifications())
            {
                evaluationManager.CurrentFilterGroupingConfiguration = GroupingConfigurator.GroupingConfiguration;

                var grouping = evaluationManager.CurrentFilterGrouping;

                if (file == null)
                {
                    var prevSelectedGroupKey = SelectedGroupKey;

                    GroupKeys.Clear();
                    GroupKeys.Add("All");

                    var newList = new AvaloniaList<IEvaluationTableViewModel.Record>();

                    foreach (var f in manager.Files)
                    {
                        if (recordMap.TryGetValue(f, out var record))
                        {
                            UpdateRecordGroup(grouping, record);

                            if (IsRecordShown(record))
                            {
                                record.IsFilteredOut = false;
                                newList.Add(record);
                            }
                            else
                            {
                                record.IsFilteredOut = true;
                            }
                        }
                    }

                    // AddRange doesn't work with DataGrid, so
                    // instead we just replace the list
                    Records = newList;

                    ResetIndices();

                    if (GroupKeys.Contains(prevSelectedGroupKey))
                    {
                        SelectedGroupKey = prevSelectedGroupKey;
                        evaluationManager.CurrentFilterGroupKey = SelectedGroupKey;
                    }
                    else
                    {
                        SelectedGroupKey = GroupKeys[0];
                        evaluationManager.CurrentFilterGroupKey = null;
                    }
                }
                else
                {
                    if (recordMap.TryGetValue(file, out var record) && record != null)
                    {
                        UpdateRecordGroup(grouping, record);

                        if (IsRecordShown(record))
                        {
                            if (record.IsFilteredOut)
                            {
                                record.IsFilteredOut = false;
                                record.Index = Records.Count;
                                Records.Add(record);
                            }
                        }
                        else
                        {
                            if (!record.IsFilteredOut)
                            {
                                record.IsFilteredOut = true;
                                Records.Remove(record);
                            }
                        }
                    }
                }
            }
        }

        private void UpdateRecordGroup(IGroupingManager.IGrouping? grouping, IEvaluationTableViewModel.Record record, IFitsImageMetadata? metadata = null)
        {
            if (grouping == null)
            {
                record.GroupKey = null;
                record.GroupMatches = null;
                return;
            }

            if (metadata == null)
            {
                metadata = manager.Get(record.File)?.Metadata;
            }
            var match = metadata != null ? grouping.GetGroupMatch(metadata) : null;

            if (match != null)
            {
                if (!GroupKeys.Contains(match.GroupKey))
                {
                    GroupKeys.Add(match.GroupKey);
                }

                record.GroupKey = match.GroupKey;
                var matchesDict = new Dictionary<string, string>();
                foreach (var pair in match.Matches)
                {
                    matchesDict.Add(pair.Key, pair.Value ?? "?");
                }
                record.GroupMatches = matchesDict;
            }
            else
            {
                record.GroupKey = null;
                record.GroupMatches = null;
            }
        }

        private bool IsRecordShown(IEvaluationTableViewModel.Record record)
        {
            return SelectedGroupKey == null || "All".Equals(SelectedGroupKey) || SelectedGroupKey.Equals(record.GroupKey);
        }

        private void UpdateRecords()
        {
            using (DelayChangeNotifications())
            {
                var newList = new AvaloniaList<IEvaluationTableViewModel.Record>();

                foreach (var f in manager.Files)
                {
                    if (recordMap.TryGetValue(f, out var record))
                    {
                        if (IsRecordShown(record))
                        {
                            record.IsFilteredOut = false;
                            newList.Add(record);
                        }
                        else
                        {
                            record.IsFilteredOut = true;
                        }
                    }
                }

                // AddRange doesn't work with DataGrid, so
                // instead we just replace the list
                Records = newList;

                ResetIndices();
            }
        }

        private void UpdateGroupingConfiguration(GroupingConfiguration configuration)
        {
            evaluationManager.CurrentFilterGroupingConfiguration = configuration;
            UpdateGroupsAndRecords();
            UpdateGraphsImmediately();
        }
    }
}
