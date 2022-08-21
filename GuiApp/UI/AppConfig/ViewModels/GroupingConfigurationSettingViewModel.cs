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
using FitsRatingTool.GuiApp.UI.Evaluation;
using ReactiveUI;
using System;

namespace FitsRatingTool.GuiApp.UI.AppConfig.ViewModels
{
    public class GroupingConfigurationSettingViewModel : SettingViewModel
    {
        public override IConfigSetting Setting { get; }

        private IJobGroupingConfiguratorViewModel _jobGroupingConfigurator;
        public IJobGroupingConfiguratorViewModel JobGroupingConfigurator
        {
            get => _jobGroupingConfigurator;
            set => this.RaiseAndSetIfChanged(ref _jobGroupingConfigurator, value);
        }

        public GroupingConfigurationSettingViewModel(string name, Func<GroupingConfiguration> getter, Action<GroupingConfiguration> setter, IJobGroupingConfiguratorViewModel.IFactory jobGroupingConfiguratorFactory) : base(name)
        {
            Setting = new ConfigSetting<GroupingConfiguration>(getter, setter);

            _jobGroupingConfigurator = jobGroupingConfiguratorFactory.Create(Setting.Value as GroupingConfiguration);

            this.WhenAnyValue(x => x.JobGroupingConfigurator.GroupingConfiguration)
                .Subscribe(x => Setting.Value = x);
        }
    }
}
