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
    public class BoolSettingViewModel : SettingViewModel
    {
        public override IConfigSetting Setting { get; }

        public BoolSettingViewModel(string name, Func<bool> getter, Action<bool> setter) : base(name)
        {
            Setting = new ConfigSetting<bool>(getter, setter);
        }
    }
}
