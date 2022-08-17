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
using System;

namespace FitsRatingTool.GuiApp.UI.AppConfig.ViewModels
{
    public class IntegerSettingViewModel : SettingViewModel
    {
        public override IConfigSetting Setting { get; }

        public int Min { get; }

        public bool HasMin => Min > int.MinValue;

        public int Max { get; }

        public bool HasMax => Max < int.MaxValue;

        public int Step { get; }

        public bool HasStep => Step > 0;

        public IntegerSettingViewModel(string name, Func<int> getter, Action<int> setter, int min, int max, int step) : base(name)
        {
            Setting = new ConfigSetting<int>(getter, setter);
            Min = min;
            Max = max;
            Step = step;
        }

        public IntegerSettingViewModel(string name, Func<int> getter, Action<int> setter) : this(name, getter, setter, int.MinValue, int.MaxValue, 0)
        {
        }
    }
}
