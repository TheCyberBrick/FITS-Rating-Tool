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
using FitsRatingTool.Exporters.Services.Impl;
using FitsRatingTool.IoC;
using FitsRatingTool.Variables.Services;
using FitsRatingTool.Variables.Services.Impl;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace FitsRatingTool.GuiApp.UI.Variables.ViewModels
{
    [Export(typeof(IComponentRegistration<IVariableConfiguratorViewModel>)), TransientReuse]
    public class ConstantVariableConfiguratorRegistration : ComponentRegistrationOfContainer<IVariableConfiguratorViewModel, IConstantVariableConfiguratorViewModel, IConstantVariableConfiguratorViewModel.Of>
    {
        public ConstantVariableConfiguratorRegistration() : base("constant", "Constant", new IConstantVariableConfiguratorViewModel.Of()) { }
    }

    [Export(typeof(IConstantVariableConfiguratorViewModel)), TransientReuse, AllowDisposableTransient]
    public class ConstantVariableConfiguratorViewModel : ViewModelBase, IConstantVariableConfiguratorViewModel
    {
        public ConstantVariableConfiguratorViewModel(IRegistrar<IConstantVariableConfiguratorViewModel, IConstantVariableConfiguratorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<ConstantVariableConfiguratorViewModel>();
        }


        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            protected set => this.RaiseAndSetIfChanged(ref _isValid, value);
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public double _value;
        public double Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }

        private bool _isNameValid;
        public bool IsNameValid
        {
            get => _isNameValid;
            set => this.RaiseAndSetIfChanged(ref _isNameValid, value);
        }


        private IConstantVariableFactory constantVariableFactory;

        private ConstantVariableConfiguratorViewModel(IVariableEditorViewModel.Of args, IConstantVariableFactory constantVariableFactory)
        {
            this.constantVariableFactory = constantVariableFactory;

            this.WhenAnyValue(x => x.Name).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.Value).Skip(1).Subscribe(x => NotifyConfigurationChange());
        }


        protected void NotifyConfigurationChange()
        {
            IsValid = true;
            Validate();
            _configurationChanged?.Invoke(this, new EventArgs());
        }

        protected virtual void Validate()
        {
            IsNameValid = Name.Length > 0 && char.IsLetter(Name[0]) && Name.All(x => char.IsLetterOrDigit(x));

            IsValid = IsNameValid;
        }

        public string CreateConfig()
        {
            var config = new ConstantVariableFactory.Config
            {
                Value = Value
            };
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        public IVariable CreateVariable()
        {
            return constantVariableFactory.Create(Name, CreateConfig());
        }

        public bool TryLoadConfig(string name, string config)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<ConstantVariableFactory.Config>(config);

                if (cfg != null)
                {
                    Name = name;
                    Value = cfg.Value;
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        private EventHandler? _configurationChanged;
        public event EventHandler ConfigurationChanged
        {
            add => _configurationChanged += value;
            remove => _configurationChanged -= value;
        }
    }
}
