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
using FitsRatingTool.GuiApp.UI.KeywordPicker;
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
    public class KeywordVariableConfiguratorViewModel : BaseVariableConfiguratorViewModel, IKeywordVariableConfiguratorViewModel
    {
        public KeywordVariableConfiguratorViewModel(IRegistrar<IKeywordVariableConfiguratorViewModel, IKeywordVariableConfiguratorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<KeywordVariableConfiguratorViewModel>();
        }


        private IKeywordPickerViewModel _keywordPicker = null!;
        public IKeywordPickerViewModel KeywordPicker
        {
            get => _keywordPicker;
            private set => this.RaiseAndSetIfChanged(ref _keywordPicker, value);
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

        private bool _isKeywordValid;
        public bool IsKeywordValid
        {
            get => _isKeywordValid;
            set => this.RaiseAndSetIfChanged(ref _isKeywordValid, value);
        }


        private IKeywordVariableFactory keywordVariableFactory;

        private KeywordVariableConfiguratorViewModel(IVariableEditorViewModel.Of args, IKeywordVariableFactory keywordVariableFactory,
            IContainer<IKeywordPickerViewModel, IKeywordPickerViewModel.OfCurrentlySelectedFile> keywordPickerContainer)
        {
            this.keywordVariableFactory = keywordVariableFactory;

            keywordPickerContainer.Singleton().Inject(new(true), this, x => x.KeywordPicker);

            this.WhenAnyValue(x => x.Keyword).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.DefaultValue).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.ExcludeFromAggregateFunctionsIfNotFound).Skip(1).Subscribe(x => NotifyConfigurationChange());

            this.WhenAnyValue(x => x.Keyword).Skip(1).Subscribe(x =>
            {
                if (!KeywordPicker.Select(x))
                {
                    KeywordPicker.SelectedKeyword = null;
                }
            });
            this.WhenAnyValue(x => x.KeywordPicker.SelectedKeyword).Skip(1).Where(x => x != null).Subscribe(x => Keyword = x ?? "");
        }


        protected override void Validate()
        {
            base.Validate();

            IsKeywordValid = Keyword.All(x => char.IsLetterOrDigit(x));

            IsValid &= IsKeywordValid;
        }

        public override string CreateConfig()
        {
            var config = new KeywordVariableFactory.Config
            {
                Keyword = Keyword,
                DefaultValue = DefaultValue,
                ExcludeFromAggregateFunctionsIfNotFound = ExcludeFromAggregateFunctionsIfNotFound
            };
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        public override IVariable CreateVariable()
        {
            return keywordVariableFactory.Create(Name, CreateConfig());
        }

        protected override bool DoTryLoadConfig(string config)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<KeywordVariableFactory.Config>(config);

                if (cfg != null)
                {
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
    }
}
