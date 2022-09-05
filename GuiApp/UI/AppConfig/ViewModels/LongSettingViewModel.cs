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

using FitsRatingTool.GuiApp.Models;
using ReactiveUI;
using System;

namespace FitsRatingTool.GuiApp.UI.AppConfig.ViewModels
{
    public class LongSettingViewModel : SettingViewModel
    {
        private readonly ConfigSetting<long> _setting;

        public override IConfigSetting Setting => _setting;

        public long Min { get; }

        public bool HasMin => Min > long.MinValue;

        public long Max { get; }

        public bool HasMax => Max < long.MaxValue;

        public long Step { get; }

        public bool HasStep => Step > 0;

        public long Denominator { get; }

        public long DisplayValue
        {
            get => _setting.Value / Denominator;
            set => _setting.Value = value * Denominator;
        }

        public LongSettingViewModel(string name, Func<long> getter, Action<long> setter, long min, long max, long step, long denominator) : base(name)
        {
            _setting = new ConfigSetting<long>(getter, setter);
            _setting.PropertyChanging += (s, e) =>
            {
                this.RaisePropertyChanging(nameof(DisplayValue));
            };
            _setting.PropertyChanged += (s, e) =>
            {
                this.RaisePropertyChanged(nameof(DisplayValue));
            };
            Min = min;
            Max = max;
            Step = step;
            Denominator = denominator == 0 ? 1 : denominator;
        }

        public LongSettingViewModel(string name, Func<long> getter, Action<long> setter) : this(name, getter, setter, long.MinValue, long.MaxValue, 0, 1)
        {
        }
    }
}
