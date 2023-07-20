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
    public class KeywordVariableConfiguratorRegistration : ComponentRegistrationOfContainer<IVariableConfiguratorViewModel, IKeywordVariableConfiguratorViewModel, IKeywordVariableConfiguratorViewModel.Of>
    {
        public KeywordVariableConfiguratorRegistration() : base("keyword", "FITS Keyword", new IKeywordVariableConfiguratorViewModel.Of()) { }
    }

    [Export(typeof(IKeywordVariableConfiguratorViewModel)), TransientReuse, AllowDisposableTransient]
    public class KeywordVariableConfiguratorViewModel : ViewModelBase, IKeywordVariableConfiguratorViewModel
    {
        public KeywordVariableConfiguratorViewModel(IRegistrar<IKeywordVariableConfiguratorViewModel, IKeywordVariableConfiguratorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<KeywordVariableConfiguratorViewModel>();
        }


        private string _name = "";
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private string _keyword = "";
        public string Keyword
        {
            get => _keyword;
            set => this.RaiseAndSetIfChanged(ref _keyword, value);
        }

        public double _defaultValue;
        public double DefaultValue
        {
            get => _defaultValue;
            set => this.RaiseAndSetIfChanged(ref _defaultValue, value);
        }

        private bool _excludeFromAggregateFunctinosIfNotFound;
        public bool ExcludeFromAggregateFunctionsIfNotFound
        {
            get => _excludeFromAggregateFunctinosIfNotFound;
            set => this.RaiseAndSetIfChanged(ref _excludeFromAggregateFunctinosIfNotFound, value);
        }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            protected set => this.RaiseAndSetIfChanged(ref _isValid, value);
        }

        private bool _isNameValid;
        public bool IsNameValid
        {
            get => _isNameValid;
            set => this.RaiseAndSetIfChanged(ref _isNameValid, value);
        }

        private bool _isKeywordValid;
        public bool IsKeywordValid
        {
            get => _isKeywordValid;
            set => this.RaiseAndSetIfChanged(ref _isKeywordValid, value);
        }


        private IKeywordVariableFactory keywordVariableFactory;

        private KeywordVariableConfiguratorViewModel(IVariableEditorViewModel.Of args, IKeywordVariableFactory keywordVariableFactory)
        {
            this.keywordVariableFactory = keywordVariableFactory;

            this.WhenAnyValue(x => x.Name).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.Keyword).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.DefaultValue).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.ExcludeFromAggregateFunctionsIfNotFound).Skip(1).Subscribe(x => NotifyConfigurationChange());
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

            IsKeywordValid = Keyword.All(x => char.IsLetterOrDigit(x));

            IsValid = IsNameValid && IsKeywordValid;
        }

        public string CreateConfig()
        {
            var config = new KeywordVariableFactory.Config
            {
                Keyword = Keyword,
                DefaultValue = DefaultValue,
                ExcludeFromAggregateFunctionsIfNotFound = ExcludeFromAggregateFunctionsIfNotFound
            };
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        public IVariable CreateVariable()
        {
            return keywordVariableFactory.Create(Name, CreateConfig());
        }

        public bool TryLoadConfig(string name, string config)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<KeywordVariableFactory.Config>(config);

                if (cfg != null)
                {
                    Name = name;
                    Keyword = cfg.Keyword;
                    DefaultValue = cfg.DefaultValue;
                    ExcludeFromAggregateFunctionsIfNotFound = cfg.ExcludeFromAggregateFunctionsIfNotFound;
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
