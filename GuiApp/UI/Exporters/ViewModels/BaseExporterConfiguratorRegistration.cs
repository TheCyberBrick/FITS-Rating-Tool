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

using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.IoC;
using System;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.UI.Exporters.ViewModels
{
    public abstract class BaseExporterConfiguratorRegistration<TConfigurator, TParameter> : ComponentRegistrationOfContainer<IExporterConfiguratorViewModel, TConfigurator, TParameter>
        where TConfigurator : class, IExporterConfiguratorViewModel
    {
        public virtual bool IsDangerous => false;

        public override bool IsEnabled => !IsDangerous || AppConfig.EnableDangerousExporters;

        [Import]
        protected IAppConfig AppConfig { get; private set; } = null!;

        protected BaseExporterConfiguratorRegistration(string id, string name, TParameter parameter) : base(id, name, parameter)
        {
        }

        protected BaseExporterConfiguratorRegistration(Func<IFactoryRoot<TConfigurator, TParameter>> rootFactory, IContainer<TConfigurator, TParameter> container, string id, string name, TParameter parameter) : base(rootFactory, container, id, name, parameter)
        {
        }
    }
}
