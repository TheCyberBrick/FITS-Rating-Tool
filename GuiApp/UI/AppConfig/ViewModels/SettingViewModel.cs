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
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.AppConfig.ViewModels
{
    public abstract class SettingViewModel : ViewModelBase, ISettingViewModel
    {
        public string Name { get; }

        public string Description { get; set; } = "";

        public bool HasDescription => Description.Length > 0;

        public ReactiveCommand<Unit, Unit> Reset { get; }

        public ReactiveCommand<Unit, Unit> Commit { get; }

        public abstract IConfigSetting Setting { get; }

        public SettingViewModel(string name)
        {
            Name = name;
            Reset = ReactiveCommand.Create(() => Setting.Reset());
            Commit = ReactiveCommand.Create(() => Setting.Commit());
        }
    }
}
