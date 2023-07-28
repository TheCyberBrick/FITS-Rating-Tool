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

using DryIocAttributes;
using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.IoC;
using FitsRatingTool.Variables.Services;
using FitsRatingTool.Variables.Services.Impl;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Variables.ViewModels
{
    [Export(typeof(IComponentRegistration<IVariableConfiguratorViewModel>)), TransientReuse]
    public class ConstantVariableConfiguratorRegistration : ComponentRegistrationOfContainer<IVariableConfiguratorViewModel, IConstantVariableConfiguratorViewModel, IConstantVariableConfiguratorViewModel.Of>
    {
        public ConstantVariableConfiguratorRegistration() : base("constant", "Constant", new IConstantVariableConfiguratorViewModel.Of()) { }
    }

    [Export(typeof(IConstantVariableConfiguratorViewModel)), TransientReuse, AllowDisposableTransient]
    public class ConstantVariableConfiguratorViewModel : BaseVariableConfiguratorViewModel, IConstantVariableConfiguratorViewModel
    {
        public ConstantVariableConfiguratorViewModel(IRegistrar<IConstantVariableConfiguratorViewModel, IConstantVariableConfiguratorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<ConstantVariableConfiguratorViewModel>();
        }


        public double _value;
        public double Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }


        private IConstantVariableFactory constantVariableFactory;

        private ConstantVariableConfiguratorViewModel(IConstantVariableConfiguratorViewModel.Of args, IConstantVariableFactory constantVariableFactory)
        {
            this.constantVariableFactory = constantVariableFactory;

            this.WhenAnyValue(x => x.Value).Skip(1).Subscribe(x => NotifyConfigurationChange());
        }


        public override string CreateConfig()
        {
            var config = new ConstantVariableFactory.Config
            {
                Value = Value
            };
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        public override IVariable CreateVariable()
        {
            return constantVariableFactory.Create(Name, CreateConfig());
        }

        protected override bool DoTryLoadConfig(string config)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<ConstantVariableFactory.Config>(config);

                if (cfg != null)
                {
                    Value = cfg.Value;
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }
    }
}
