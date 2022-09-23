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
using ReactiveUI;
using System.Collections.Generic;
using System.IO;
using FitsRatingTool.GuiApp.UI.FitsImage;
using System.Reactive;
using System.Collections;

namespace FitsRatingTool.GuiApp.UI.Evaluation
{
    public interface IEvaluationTableViewModel
    {
        public interface IFactory
        {
            IEvaluationTableViewModel Create();
        }

        #region +++ Records +++
        public class Record : ReactiveObject
        {
            public long Id { get; }

            public long IdPlusOne => Id + 1;

            public string File { get; }

            public string FileName { get; }

            private int _index;
            public int Index
            {
                get => _index;
                set
                {
                    this.RaiseAndSetIfChanged(ref _index, value);
                    this.RaiseAndSetIfChanged(ref _indexPlusOne, value + 1, nameof(IndexPlusOne));
                }
            }

            private int _indexPlusOne;
            public int IndexPlusOne
            {
                get => _indexPlusOne;
            }

            private IFitsImageStatisticsViewModel? _statistics;
            public IFitsImageStatisticsViewModel? Statistics
            {
                get => _statistics;
                set => this.RaiseAndSetIfChanged(ref _statistics, value);
            }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => this.RaiseAndSetIfChanged(ref _isSelected, value);
            }

            private string? _groupKey;
            public string? GroupKey
            {
                get => _groupKey;
                set => this.RaiseAndSetIfChanged(ref _groupKey, value);
            }

            private IReadOnlyDictionary<string, string>? _groupMatches;
            public IReadOnlyDictionary<string, string>? GroupMatches
            {
                get => _groupMatches;
                set => this.RaiseAndSetIfChanged(ref _groupMatches, value);
            }

            private bool _isFilteredOut;
            public bool IsFilteredOut
            {
                get => _isFilteredOut;
                set => this.RaiseAndSetIfChanged(ref _isFilteredOut, value);
            }

            private bool _isOutdated;
            public bool IsOutdated
            {
                get => _isOutdated;
                set => this.RaiseAndSetIfChanged(ref _isOutdated, value);
            }

            public ReactiveCommand<Unit, Unit> Remove { get; }

            public Record(long id, string file, IFitsImageStatisticsViewModel? stats)
            {
                Id = id;
                File = file;
                FileName = Path.GetFileName(file);
                Statistics = stats;
                Remove = ReactiveCommand.Create(() => { });
            }
        }

        AvaloniaList<Record> Records { get; }

        Record? SelectedRecord { get; }

        ReactiveCommand<IEnumerable, Unit> RemoveRecords { get; }
        #endregion

        #region +++ Graph +++
        double GraphRangeInMADSigmas { get; set; }

        double GraphMin { get; }

        double GraphMax { get; }

        double RatingGraphMin { get; }

        double RatingGraphMax { get; }

        public struct SigmaRange
        {
            public double Min { get; private set; }
            public double Max { get; private set; }
            public double RangeLower { get; private set; }
            public double RangeUpper { get; private set; }
            public double Range { get; private set; }

            public SigmaRange(double min, double max, double rangeLower, double rangeUpper, double range)
            {
                Min = min;
                Max = max;
                RangeLower = rangeLower;
                RangeUpper = rangeUpper;
                Range = range;
            }
        }

        SigmaRange ZeroSigmaRange { get; }

        SigmaRange HalfSigmaRange { get; }

        SigmaRange OneSigmaRange { get; }

        SigmaRange TwoSigmaRange { get; }

        SigmaRange ThreeSigmaRange { get; }

        SigmaRange RatingZeroSigmaRange { get; }

        SigmaRange RatingHalfSigmaRange { get; }

        SigmaRange RatingOneSigmaRange { get; }

        SigmaRange RatingTwoSigmaRange { get; }

        SigmaRange RatingThreeSigmaRange { get; }
        #endregion

        #region +++ Data Selection +++
        int DataKeyIndex { get; }
        #endregion

        #region +++ Grouping +++
        AvaloniaList<string> GroupKeys { get; }

        string SelectedGroupKey { get; set; }

        IJobGroupingConfiguratorViewModel GroupingConfigurator { get; }
        #endregion

        ReactiveCommand<Unit, IEvaluationExporterViewModel> ShowEvaluationExporter { get; }
    }
}
