﻿/*
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

using DryIocAttributes;
using FitsRatingTool.GuiApp.Services;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.UI.AppConfig.ViewModels
{
    [Export(typeof(IAppConfigCategoryViewModel)), TransientReuse]
    public class AppConfigCategoryViewModel : IAppConfigCategoryViewModel
    {
        public AppConfigCategoryViewModel(IRegistrar<IAppConfigCategoryViewModel, IAppConfigCategoryViewModel.OfName> reg)
        {
            reg.RegisterAndReturn<AppConfigCategoryViewModel>();
        }

        public string Name { get; }

        public List<ISettingViewModel> Settings { get; } = new List<ISettingViewModel>();

        public AppConfigCategoryViewModel(IAppConfigCategoryViewModel.OfName args)
        {
            Name = args.Name;
        }
    }
}
