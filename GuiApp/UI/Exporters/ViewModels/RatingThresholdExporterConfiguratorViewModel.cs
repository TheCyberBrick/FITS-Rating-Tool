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

using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Exporters.ViewModels
{
    public abstract class RatingThresholdExporterConfiguratorViewModel<T> : BaseExporterConfiguratorViewModel
        where T : struct
    {
        private bool _isMinRatingThresholdEnabled;
        public bool IsMinRatingThresholdEnabled
        {
            get => _isMinRatingThresholdEnabled;
            set => this.RaiseAndSetIfChanged(ref _isMinRatingThresholdEnabled, value);
        }

        private T _minRatingThreshold;
        public T MinRatingThreshold
        {
            get => _minRatingThreshold;
            set => this.RaiseAndSetIfChanged(ref _minRatingThreshold, value);
        }

        private bool _isMaxRatingThresholdEnabled;
        public bool IsMaxRatingThresholdEnabled
        {
            get => _isMaxRatingThresholdEnabled;
            set => this.RaiseAndSetIfChanged(ref _isMaxRatingThresholdEnabled, value);
        }

        private T _maxRatingThreshold;
        public T MaxRatingThreshold
        {
            get => _maxRatingThreshold;
            set => this.RaiseAndSetIfChanged(ref _maxRatingThreshold, value);
        }

        private ObservableAsPropertyHelper<bool> _isLessThanRule;
        public bool IsLessThanRule => _isLessThanRule.Value;

        private ObservableAsPropertyHelper<bool> _isGreaterThanRule;
        public bool IsGreaterThanRule => _isGreaterThanRule.Value;

        private ObservableAsPropertyHelper<bool> _isLessThanOrGreaterThanRule;
        public bool IsLessThanOrGreaterThanRule => _isLessThanOrGreaterThanRule.Value;


        public RatingThresholdExporterConfiguratorViewModel()
        {
            var hasMin = this.WhenAnyValue(x => x.IsMinRatingThresholdEnabled);
            var hasMax = this.WhenAnyValue(x => x.IsMaxRatingThresholdEnabled);

            hasMin.Subscribe(x => NotifyConfigurationChange());
            hasMax.Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.MaxRatingThreshold).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.MinRatingThreshold).Subscribe(x => NotifyConfigurationChange());

            _isLessThanRule = Observable.CombineLatest(hasMin, hasMax, (a, b) => a && !b).ToProperty(this, x => x.IsLessThanRule);
            _isGreaterThanRule = Observable.CombineLatest(hasMin, hasMax, (a, b) => !a && b).ToProperty(this, x => x.IsGreaterThanRule);
            _isLessThanOrGreaterThanRule = Observable.CombineLatest(hasMin, hasMax, (a, b) => a && b).ToProperty(this, x => x.IsLessThanOrGreaterThanRule);
        }

        protected override void Validate()
        {
            base.Validate();
            IsValid &= IsMinRatingThresholdEnabled || IsMaxRatingThresholdEnabled;
        }
    }
}
