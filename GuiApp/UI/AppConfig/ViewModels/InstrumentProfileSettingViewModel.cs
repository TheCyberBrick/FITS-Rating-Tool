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
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using ReactiveUI;
using System;

namespace FitsRatingTool.GuiApp.UI.AppConfig.ViewModels
{
    public class InstrumentProfileSettingViewModel : SettingViewModel
    {
        public override IConfigSetting Setting { get; }

        private IInstrumentProfileSelectorViewModel _instrumentProfileSelector;
        public IInstrumentProfileSelectorViewModel InstrumentProfileSelector
        {
            get => _instrumentProfileSelector;
            set => this.RaiseAndSetIfChanged(ref _instrumentProfileSelector, value);
        }

        public InstrumentProfileSettingViewModel(string name, Func<string> getter, Action<string> setter, IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory) : base(name)
        {
            Setting = new ConfigSetting<string>(getter, setter);

            _instrumentProfileSelector = instrumentProfileSelectorFactory.Create();
            _instrumentProfileSelector.SelectedProfile = null;

            var currentId = Setting.Value as string;
            if (currentId != null)
            {
                _instrumentProfileSelector.SelectById(currentId);
            }

            Setting.Value = _instrumentProfileSelector.SelectedProfile?.Id ?? "";

            this.WhenAnyValue(x => x.InstrumentProfileSelector.SelectedProfile)
                .Subscribe(x => Setting.Value = x?.Id ?? "");
        }
    }
}
