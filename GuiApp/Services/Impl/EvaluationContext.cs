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

using Avalonia.Utilities;
using DryIocAttributes;
using FitsRatingTool.Common.Models.Instrument;
using FitsRatingTool.Common.Services;
using FitsRatingTool.GuiApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    [Export(typeof(IEvaluationContext)), CurrentScopeReuse(AppScopes.Context.Evaluation)]
    public class EvaluationContext : IEvaluationContext
    {
        private bool isCachedEvaluatorFormulaValid;
        private string? cachedEvaluatorFormula;

        private readonly IGroupingManager groupingManager;
        private readonly IAppConfig appConfig;
        private readonly IEvaluationService evaluationService;

        private readonly IVariableContext variableContext;
        private readonly IInstrumentProfileContext instrumentProfileContext;

        public EvaluationContext(IGroupingManager groupingManager, IAppConfig appConfig, IEvaluationService evaluationService,
            IVariableContext variableContext, IInstrumentProfileContext instrumentProfileContext)
        {
            this.groupingManager = groupingManager;
            this.appConfig = appConfig;
            this.evaluationService = evaluationService;

            this.variableContext = variableContext;
            this.instrumentProfileContext = instrumentProfileContext;

            WeakEventHandlerManager.Subscribe<IVariableContext, IVariableContext.VariablesChangedEventArgs, EvaluationContext>(variableContext, nameof(variableContext.CurrentVariablesChanged), OnCurrentVariablesChanged);
        }

        public void LoadFromConfig()
        {
            var file = appConfig.DefaultEvaluationFormulaPath;
            if (file.Length > 0)
            {
                try
                {
                    CurrentFormula = File.ReadAllText(file);
                }
                catch (Exception)
                {
                    CurrentFormula = $"Could not load default formula from file '{file}'. Please make sure it exists and is readable or adjust the default evaluation formula setting.";
                }
            }
            else
            {
                CurrentFormula = "";
            }
        }

        public void LoadFromCurrentProfile()
        {
            LoadFromCurrentProfile(instrumentProfileContext);
        }

        public void LoadFromCurrentProfile(IInstrumentProfileContext ctx)
        {
            var currentProfile = ctx.CurrentProfile;

            var groupingKeys = currentProfile?.GroupingKeys;
            if (groupingKeys != null && GroupingConfiguration.TryParseGroupingKeys(groupingManager, groupingKeys, out var groupingConfiguration))
            {
                CurrentGroupingConfiguration = CurrentFilterGroupingConfiguration = groupingConfiguration;
            }
            else
            {
                CurrentGroupingConfiguration = CurrentFilterGroupingConfiguration = null;
            }

            LoadedInstrumentProfile = currentProfile;
        }

        private void ResetLoadedInstrumentProfileContext()
        {
            LoadedInstrumentProfile = null;
        }

        public void LoadFromOther(IEvaluationContext ctx)
        {
            CurrentGroupingConfiguration = ctx.CurrentGroupingConfiguration;
            CurrentFilterGroupingConfiguration = ctx.CurrentFilterGroupingConfiguration;
            CurrentFilterGroupKey = ctx.CurrentFilterGroupKey;
            CurrentFormula = ctx.CurrentFormula;
        }

        private void OnCurrentVariablesChanged(object? sender, IVariableContext.VariablesChangedEventArgs args)
        {
            InvalidateCachedEvaluator();
        }

        private void InvalidateCachedEvaluator()
        {
            CurrentEvaluator = null;
            cachedEvaluatorFormula = null;
            UpdateEvaluator();
        }

        private void UpdateEvaluator()
        {
            IEvaluationService.IEvaluator? evaluatorInstance = null;

            var variables = variableContext.CurrentVariables;

            if (evaluatorInstance == null)
            {
                if (cachedEvaluatorFormula != null && cachedEvaluatorFormula.Equals(CurrentFormula) && isCachedEvaluatorFormulaValid)
                {
                    evaluatorInstance = CurrentEvaluator;
                }
                else
                {
                    CurrentEvaluator = null;
                    cachedEvaluatorFormula = CurrentFormula;

                    if (cachedEvaluatorFormula != null && evaluationService.Build(cachedEvaluatorFormula, variables, out var newEvaluatorInstance, out var _) && newEvaluatorInstance != null)
                    {
                        evaluatorInstance = CurrentEvaluator = newEvaluatorInstance;
                        isCachedEvaluatorFormulaValid = true;
                    }
                    else
                    {
                        isCachedEvaluatorFormulaValid = false;
                    }
                }
            }

            IsCurrentFormulaValid = evaluatorInstance != null;
        }

        public bool IsCurrentFormulaValid { get; private set; }

        private IGroupingManager.IGrouping? _currentGrouping;
        public IGroupingManager.IGrouping? CurrentGrouping
        {
            get => _currentGrouping;
            private set
            {
                if (_currentGrouping != value)
                {
                    var old = _currentGrouping;
                    _currentGrouping = value;
                    _currentGroupingChanged?.Invoke(this, new IEvaluationContext.GroupingChangedEventArgs(old, value));
                }
            }
        }

        private IGroupingManager.IGrouping? _currentFilterGrouping;
        public IGroupingManager.IGrouping? CurrentFilterGrouping
        {
            get => _currentFilterGrouping;
            private set
            {
                if (_currentFilterGrouping != value)
                {
                    var old = _currentFilterGrouping;
                    _currentFilterGrouping = value;
                    _currentFilterGroupingChanged?.Invoke(this, new IEvaluationContext.GroupingChangedEventArgs(old, value));
                }
            }
        }

        private string? _currentFilterGroupKey;
        public string? CurrentFilterGroupKey
        {
            get => _currentFilterGroupKey;
            set
            {
                if (!string.Equals(_currentFilterGroupKey, value))
                {
                    var old = _currentFilterGroupKey;
                    _currentFilterGroupKey = value;
                    _currentFilterGroupKeyChanged?.Invoke(this, new IEvaluationContext.GroupKeyChangedEventArgs(old, value));
                }
            }
        }

        private string? _currentFormula;
        public string? CurrentFormula
        {
            get => _currentFormula;
            set
            {
                if (!string.Equals(_currentFormula, value))
                {
                    var old = _currentFormula;
                    _currentFormula = value;

                    ResetLoadedInstrumentProfileContext();
                    UpdateEvaluator();

                    _currentFormulaChanged?.Invoke(this, new IEvaluationContext.FormulaChangedEventArgs(old, value));
                }
            }
        }

        public IEvaluationService.IEvaluator? CurrentEvaluator { get; private set; }

        private GroupingConfiguration? _currentGroupingConfiguration;
        public GroupingConfiguration? CurrentGroupingConfiguration
        {
            get => _currentGroupingConfiguration;
            set
            {
                if (!EqualityComparer<GroupingConfiguration?>.Default.Equals(_currentGroupingConfiguration, value))
                {
                    var old = _currentGroupingConfiguration;
                    _currentGroupingConfiguration = value;

                    ResetLoadedInstrumentProfileContext();

                    _currentGroupingConfigurationChanged?.Invoke(this, new IEvaluationContext.GroupingConfigurationChangedEventArgs(old, value));

                    if (value != null)
                    {
                        var grouping = groupingManager.BuildGrouping(value.GroupingKeys.ToArray());
                        CurrentGrouping = grouping.IsEmpty ? null : grouping;
                    }
                    else
                    {
                        CurrentGrouping = null;
                    }
                }
            }
        }

        private GroupingConfiguration? _currentFilterGroupingConfiguration;
        public GroupingConfiguration? CurrentFilterGroupingConfiguration
        {
            get => _currentFilterGroupingConfiguration;
            set
            {
                if (!EqualityComparer<GroupingConfiguration?>.Default.Equals(_currentFilterGroupingConfiguration, value))
                {
                    var old = _currentFilterGroupingConfiguration;
                    _currentFilterGroupingConfiguration = value;
                    _currentFilterGroupingConfigurationChanged?.Invoke(this, new IEvaluationContext.GroupingConfigurationChangedEventArgs(old, value));

                    if (value != null)
                    {
                        var grouping = groupingManager.BuildGrouping(value.GroupingKeys.ToArray());
                        CurrentFilterGrouping = grouping.IsEmpty ? null : grouping;
                    }
                    else
                    {
                        CurrentFilterGrouping = null;
                    }
                }
            }
        }

        private IReadOnlyInstrumentProfile? _loadedInstrumentProfile;
        public IReadOnlyInstrumentProfile? LoadedInstrumentProfile
        {
            get => _loadedInstrumentProfile;
            set
            {
                if (_loadedInstrumentProfile != value)
                {
                    var old = _loadedInstrumentProfile;
                    _loadedInstrumentProfile = value;
                    _loadedInstrumentProfileChanged?.Invoke(this, new IEvaluationContext.InstrumentProfileChangedEventArgs(old, value));
                }
            }
        }

        private event EventHandler<IEvaluationContext.GroupingChangedEventArgs>? _currentGroupingChanged;
        public event EventHandler<IEvaluationContext.GroupingChangedEventArgs> CurrentGroupingChanged
        {
            add
            {
                _currentGroupingChanged += value;
            }
            remove
            {
                _currentGroupingChanged -= value;
            }
        }

        private event EventHandler<IEvaluationContext.GroupingChangedEventArgs>? _currentFilterGroupingChanged;
        public event EventHandler<IEvaluationContext.GroupingChangedEventArgs> CurrentFilterGroupingChanged
        {
            add
            {
                _currentFilterGroupingChanged += value;
            }
            remove
            {
                _currentFilterGroupingChanged -= value;
            }
        }

        private event EventHandler<IEvaluationContext.GroupKeyChangedEventArgs>? _currentFilterGroupKeyChanged;
        public event EventHandler<IEvaluationContext.GroupKeyChangedEventArgs> CurrentFilterGroupKeyChanged
        {
            add
            {
                _currentFilterGroupKeyChanged += value;
            }
            remove
            {
                _currentFilterGroupKeyChanged -= value;
            }
        }

        private event EventHandler<IEvaluationContext.GroupingConfigurationChangedEventArgs>? _currentGroupingConfigurationChanged;
        public event EventHandler<IEvaluationContext.GroupingConfigurationChangedEventArgs> CurrentGroupingConfigurationChanged
        {
            add
            {
                _currentGroupingConfigurationChanged += value;
            }
            remove
            {
                _currentGroupingConfigurationChanged -= value;
            }
        }

        private event EventHandler<IEvaluationContext.GroupingConfigurationChangedEventArgs>? _currentFilterGroupingConfigurationChanged;
        public event EventHandler<IEvaluationContext.GroupingConfigurationChangedEventArgs> CurrentFilterGroupingConfigurationChanged
        {
            add
            {
                _currentFilterGroupingConfigurationChanged += value;
            }
            remove
            {
                _currentFilterGroupingConfigurationChanged -= value;
            }
        }

        private event EventHandler<IEvaluationContext.FormulaChangedEventArgs>? _currentFormulaChanged;
        public event EventHandler<IEvaluationContext.FormulaChangedEventArgs> CurrentFormulaChanged
        {
            add
            {
                _currentFormulaChanged += value;
            }
            remove
            {
                _currentFormulaChanged -= value;
            }
        }

        private event EventHandler<IEvaluationContext.InstrumentProfileChangedEventArgs>? _loadedInstrumentProfileChanged;
        public event EventHandler<IEvaluationContext.InstrumentProfileChangedEventArgs> LoadedInstrumentProfileChanged
        {
            add
            {
                _loadedInstrumentProfileChanged += value;
            }
            remove
            {
                _loadedInstrumentProfileChanged -= value;
            }
        }
    }
}
