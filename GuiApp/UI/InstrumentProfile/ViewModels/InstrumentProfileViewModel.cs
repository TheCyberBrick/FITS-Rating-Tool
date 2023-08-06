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

using Avalonia.Collections;
using DryIoc;
using DryIocAttributes;
using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.Common.Models.Instrument;
using FitsRatingTool.Common.Services;
using FitsRatingTool.Common.Services.Impl;
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Evaluation;
using FitsRatingTool.GuiApp.UI.Utils;
using FitsRatingTool.GuiApp.UI.Variables;
using FitsRatingTool.IoC;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using static FitsRatingTool.GuiApp.UI.InstrumentProfile.IInstrumentProfileViewModel;

namespace FitsRatingTool.GuiApp.UI.InstrumentProfile.ViewModels
{
    [Export(typeof(IInstrumentProfileViewModel)), TransientReuse]
    public class InstrumentProfileViewModel : ViewModelBase, IInstrumentProfileViewModel
    {
        public InstrumentProfileViewModel(IRegistrar<IInstrumentProfileViewModel, IInstrumentProfileViewModel.OfProfile> reg)
        {
            reg.RegisterAndReturn<InstrumentProfileViewModel>();
        }

        private class VariableItemViewModel : ViewModelBase, IInstrumentProfileViewModel.IVariableItemViewModel
        {
            public IVariableEditorViewModel Editor { get; }

            public ReactiveCommand<Unit, Unit> Remove { get; }


            public VariableItemViewModel(IVariableEditorViewModel editor)
            {
                Editor = editor;
                Remove = ReactiveCommand.Create(() => { });
            }
        }

        private string _id = "";
        public string Id
        {
            get => _id;
            set => this.RaiseAndSetIfChanged(ref _id, value);
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        private string _key = "";
        public string Key
        {
            get => _key;
            set => this.RaiseAndSetIfChanged(ref _key, value);
        }

        private float? _focalLength;
        public float? FocalLength
        {
            get => _focalLength;
            set => this.RaiseAndSetIfChanged(ref _focalLength, value);
        }

        private int? _bitDepth;
        public int? BitDepth
        {
            get => _bitDepth;
            set => this.RaiseAndSetIfChanged(ref _bitDepth, value);
        }

        private float? _electronsPerADU;
        public float? ElectronsPerADU
        {
            get => _electronsPerADU;
            set => this.RaiseAndSetIfChanged(ref _electronsPerADU, value);
        }

        private float? _pixelSizeInMicrons;
        public float? PixelSizeInMicrons
        {
            get => _pixelSizeInMicrons;
            set => this.RaiseAndSetIfChanged(ref _pixelSizeInMicrons, value);
        }



        private bool _isFocalLengthEnabled;
        public bool IsFocalLengthEnabled
        {
            get => _isFocalLengthEnabled;
            set => this.RaiseAndSetIfChanged(ref _isFocalLengthEnabled, value);
        }

        private bool _isBitDepthEnabled;
        public bool IsBitDepthEnabled
        {
            get => _isBitDepthEnabled;
            set => this.RaiseAndSetIfChanged(ref _isBitDepthEnabled, value);
        }

        private bool _isElectronsPerADUEnabled;
        public bool IsElectronsPerADUEnabled
        {
            get => _isElectronsPerADUEnabled;
            set => this.RaiseAndSetIfChanged(ref _isElectronsPerADUEnabled, value);
        }

        private bool _isPixelSizeInMicronsEnabled;
        public bool IsPixelSizeInMicronsEnabled
        {
            get => _isPixelSizeInMicronsEnabled;
            set => this.RaiseAndSetIfChanged(ref _isPixelSizeInMicronsEnabled, value);
        }


        private IJobGroupingConfiguratorViewModel _jobGroupingConfiguratorViewModel = null!;
        public IJobGroupingConfiguratorViewModel GroupingConfigurator
        {
            get => _jobGroupingConfiguratorViewModel;
            set => this.RaiseAndSetIfChanged(ref _jobGroupingConfiguratorViewModel, value);

        }

        public AvaloniaList<IVariableItemViewModel> Variables { get; } = new();


        private bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set => this.RaiseAndSetIfChanged(ref _isModified, value);
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
        }

        public bool IsNew { get; }

        private IReadOnlyInstrumentProfile? _sourceProfile;
        public IReadOnlyInstrumentProfile? SourceProfile
        {
            get => _sourceProfile;
            private set => this.RaiseAndSetIfChanged(ref _sourceProfile, value);
        }

        private readonly ObservableAsPropertyHelper<bool> _isIdValid;
        public bool IsIdValid => _isIdValid.Value;

        private readonly ObservableAsPropertyHelper<bool> _isIdAvailable;
        public bool IsIdAvailable => _isIdAvailable.Value;

        private readonly ObservableAsPropertyHelper<bool> _isValid;
        public bool IsValid => _isValid.Value;

        private bool _isVariableValid;
        private bool IsVariableValid
        {
            get => _isVariableValid;
            set => this.RaiseAndSetIfChanged(ref _isVariableValid, value);
        }

        public ReactiveCommand<Unit, Unit> AddVariable { get; }

        public ReactiveCommand<Unit, Unit> Reset { get; }



        private readonly IInstrumentProfileManager instrumentProfileManager;
        private readonly IInstrumentProfileFactory instrumentProfileFactory;
        private readonly IGroupingManager groupingManager;


        private readonly IContainer<IVariableEditorViewModel, IVariableEditorViewModel.Of> variableEditorContainer;
        private readonly ISingletonContainer<IJobGroupingConfiguratorViewModel, IJobGroupingConfiguratorViewModel.OfConfiguration> jobGroupingConfiguratorContainer;

        private InstrumentProfileViewModel(IInstrumentProfileViewModel.OfProfile args, IInstrumentProfileManager instrumentProfileManager,
            IInstrumentProfileFactory instrumentProfileFactory, IGroupingManager groupingManager,
            IContainer<IVariableEditorViewModel, IVariableEditorViewModel.Of> variableEditorContainer,
            IContainer<IJobGroupingConfiguratorViewModel, IJobGroupingConfiguratorViewModel.OfConfiguration> jobGroupingConfiguratorContainer)
        {
            this.instrumentProfileManager = instrumentProfileManager;
            this.instrumentProfileFactory = instrumentProfileFactory;
            this.groupingManager = groupingManager;

            this.variableEditorContainer = variableEditorContainer;
            this.jobGroupingConfiguratorContainer = jobGroupingConfiguratorContainer.Singleton();

            jobGroupingConfiguratorContainer.Inject(new(), this, x => x.GroupingConfigurator);

            IsNew = args.Profile == null;

            SourceProfile = args.Profile;

            AddVariable = ReactiveCommand.Create(() =>
            {
                CreateVariable();
            });

            Reset = ReactiveCommand.Create(() =>
            {
                ResetToSourceProfile();
            }, Observable.CombineLatest(Observable.Return(!IsNew), this.WhenAnyValue(x => x.IsModified), (a, b) => a && b));

            var isIdAvailable = this.WhenAnyValue(x => x.Id, x => (!IsNew && x == args.Profile?.Id) || !instrumentProfileManager.Contains(x));
            _isIdAvailable = isIdAvailable.ToProperty(this, x => x.IsIdAvailable);

            var isIdValidated = this.WhenAnyValue(x => x.Id, ValidateId);
            _isIdValid = Observable.CombineLatest(isIdValidated, isIdAvailable, (a, b) => a && b).ToProperty(this, x => x.IsIdValid);

            _isValid = Observable.CombineLatest(this.WhenAnyValue(x => x.IsIdValid), this.WhenAnyValue(x => x.IsVariableValid), (a, b) => a && b).ToProperty(this, x => x.IsValid);

            this.WhenAnyValue(x => x.IsFocalLengthEnabled).Select(x => !x).Subscribe(_ => FocalLength = null);
            this.WhenAnyValue(x => x.IsBitDepthEnabled).Select(x => !x).Subscribe(_ => BitDepth = null);
            this.WhenAnyValue(x => x.IsElectronsPerADUEnabled).Select(x => !x).Subscribe(_ => ElectronsPerADU = null);
            this.WhenAnyValue(x => x.IsPixelSizeInMicronsEnabled).Select(x => !x).Subscribe(_ => PixelSizeInMicrons = null);

            this.WhenAnyValue(x => x.Id).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.Name).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.Description).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.Key).Subscribe(_ => IsModified = true);

            this.WhenAnyValue(x => x.IsFocalLengthEnabled).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.FocalLength).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.IsBitDepthEnabled).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.BitDepth).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.IsElectronsPerADUEnabled).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.ElectronsPerADU).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.IsPixelSizeInMicronsEnabled).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.PixelSizeInMicrons).Subscribe(_ => IsModified = true);

            this.WhenAnyValue(x => x.GroupingConfigurator.GroupingConfiguration).Subscribe(_ => IsModified = true);

            Variables.CollectionChanged += (sender, e) =>
            {
                IsModified = true;
                ValidateVariables();
            };

            ValidateVariables();
        }


        protected override void OnInstantiated()
        {
            if (SourceProfile != null)
            {
                LoadFromProfile(SourceProfile);
            }
        }

        private bool ValidateId(string id)
        {
            return id.Length > 0 && id.All(x => char.IsLetterOrDigit(x) || x == '_');
        }

        private void LoadFromProfile(IReadOnlyInstrumentProfile profile)
        {
            // Fetch new profile values if available
            var newProfile = instrumentProfileManager.Get(profile.Id)?.Profile;
            if (newProfile != null)
            {
                SourceProfile = profile = newProfile;
            }

            using (DelayChangeNotifications())
            {
                Id = profile.Id;
                Name = profile.Name;
                Description = profile.Description;
                Key = profile.Key;
            }

            if (profile.FocalLength != null)
            {
                IsFocalLengthEnabled = true;
                FocalLength = profile.FocalLength;
            }
            else
            {
                IsFocalLengthEnabled = false;
            }

            if (profile.BitDepth != null)
            {
                IsBitDepthEnabled = true;
                BitDepth = profile.BitDepth;
            }
            else
            {
                IsBitDepthEnabled = false;
            }

            if (profile.ElectronsPerADU != null)
            {
                IsElectronsPerADUEnabled = true;
                ElectronsPerADU = profile.ElectronsPerADU;
            }
            else
            {
                IsElectronsPerADUEnabled = false;
            }

            if (profile.PixelSizeInMicrons != null)
            {
                IsPixelSizeInMicronsEnabled = true;
                PixelSizeInMicrons = profile.PixelSizeInMicrons;
            }
            else
            {
                IsPixelSizeInMicronsEnabled = false;
            }


            // Load grouping
            var groupingKeys = profile.GroupingKeys;
            if (groupingKeys != null && GroupingConfiguration.TryParseGroupingKeys(groupingManager, groupingKeys, out var grouping))
            {
                GroupingConfigurator = jobGroupingConfiguratorContainer.Instantiate(new(grouping));
            }
            else
            {
                GroupingConfigurator = jobGroupingConfiguratorContainer.Instantiate(new());
            }


            // Remove all variables first
            while (Variables.Count > 0)
            {
                RemoveVariable(Variables[Variables.Count - 1]);
            }
            // And then try loading them from configs
            if (profile.Variables != null)
            {
                foreach (var config in profile.Variables)
                {
                    var item = CreateVariable();

                    bool loaded = false;

                    try
                    {
                        loaded = item.Editor.Configure(config.Id, configurator =>
                        {
                            return configurator.TryLoadConfig(config.Name, config.Config);
                        });
                    }
                    finally
                    {
                        if (!loaded)
                        {
                            RemoveVariable(item);
                        }
                    }
                }
            }

            IsModified = false;

            ValidateVariables();
        }

        private IVariableItemViewModel CreateVariable()
        {
            var item = new VariableItemViewModel(variableEditorContainer.Instantiate(new()));

            item.Remove.Subscribe(_ =>
            {
                RemoveVariable(item);
            });

            SubscribeToEvent<IItemEditorViewModel<IVariableConfiguratorViewModel>, EventArgs, InstrumentProfileViewModel>(item.Editor, nameof(item.Editor.ConfigurationChanged), OnVariableConfiguratorChanged);

            Variables.Add(item);

            return item;
        }

        private void RemoveVariable(IVariableItemViewModel item)
        {
            Variables.Remove(item);

            variableEditorContainer.Destroy(item.Editor);

            UnsubscribeFromEvent<IItemEditorViewModel<IVariableConfiguratorViewModel>, EventArgs, InstrumentProfileViewModel>(item.Editor, nameof(item.Editor.ConfigurationChanged), OnVariableConfiguratorChanged);
        }

        private void OnVariableConfiguratorChanged(object? sender, EventArgs args)
        {
            IsModified = true;

            ValidateVariables();
        }

        public bool ResetToSourceProfile()
        {
            if (SourceProfile != null)
            {
                LoadFromProfile(SourceProfile);
                return true;
            }
            return false;
        }

        private void ValidateVariables()
        {
            bool valid = true;
            foreach (var variable in Variables)
            {
                if (!variable.Editor.IsValid)
                {
                    valid = false;
                    break;
                }
            }
            IsVariableValid = valid;
        }

        public IReadOnlyInstrumentProfile CreateProfile()
        {
            if (!IsValid)
            {
                throw new Exception("Invalid profile");
            }

            var profile = instrumentProfileFactory.Builder().Id(Id).Build();

            profile.Name = Name;
            profile.Description = Description;
            profile.Key = Key;

            profile.FocalLength = FocalLength;
            profile.BitDepth = BitDepth;
            profile.ElectronsPerADU = ElectronsPerADU;
            profile.PixelSizeInMicrons = PixelSizeInMicrons;

            profile.GroupingKeys = GroupingConfigurator.GroupingConfiguration.GroupingKeys;

            if (Variables != null)
            {
                var variables = new List<IReadOnlyJobConfig.VariableConfig>();

                foreach (var item in Variables)
                {
                    var configurator = item.Editor.Configurator;
                    var selectorItem = item.Editor.Selector.SelectedItem;
                    if (configurator != null && selectorItem != null)
                    {
                        var variable = configurator.CreateVariable();
                        var config = configurator.CreateConfig();
                        variables.Add(new IReadOnlyJobConfig.VariableConfig(selectorItem.Id, variable.Name, config));
                    }
                }

                profile.Variables = variables;
            }

            return profile;
        }
    }
}
