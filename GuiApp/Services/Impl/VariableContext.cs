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
using FitsRatingTool.Common.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    [Export(typeof(IVariableContext)), CurrentScopeReuse(AppScopes.Context.Variable)]
    public class VariableContext : IVariableContext
    {
        private IReadOnlyList<IReadOnlyJobConfig.VariableConfig>? _currentVariableConfigs;
        public IReadOnlyList<IReadOnlyJobConfig.VariableConfig>? CurrentVariableConfigs
        {
            get => _currentVariableConfigs;
            set
            {
                var old = _currentVariableConfigs;
                _currentVariableConfigs = value;
                _currentVariableConfigsChanged?.Invoke(this, new IVariableContext.VariableConfigsChangedEventArgs(old, value));
                SyncVariables();
            }
        }

        public IReadOnlyList<IReadOnlyJobConfig.VariableConfig>? InvalidVariableConfigs { get; private set; }

        private IReadOnlyList<IReadOnlyVariable>? _currentVariables;
        public IReadOnlyList<IReadOnlyVariable>? CurrentVariables
        {
            get => _currentVariables;
            set
            {
                var old = _currentVariables;
                _currentVariables = value;
                _currentVariablesChanged?.Invoke(this, new IVariableContext.VariablesChangedEventArgs(old, value));
            }
        }


        private readonly IVariableManager variableManager;

        private readonly IInstrumentProfileContext instrumentProfileContext;

        public VariableContext(IVariableManager variableManager, IInstrumentProfileContext instrumentProfileContext)
        {
            this.variableManager = variableManager;
            this.instrumentProfileContext = instrumentProfileContext;
        }

        public void LoadFromOther(IVariableContext ctx)
        {
            CurrentVariableConfigs = ctx.CurrentVariableConfigs;
        }

        public void LoadFromCurrentProfile()
        {
            LoadFromCurrentProfile(instrumentProfileContext);
        }

        public void LoadFromCurrentProfile(IInstrumentProfileContext ctx)
        {
            CurrentVariableConfigs = ctx.CurrentProfile?.Variables;
        }

        private void SyncVariables()
        {
            var currentVariableConfigs = _currentVariableConfigs;

            if (currentVariableConfigs != null)
            {
                List<IReadOnlyJobConfig.VariableConfig>? invalidVariableConfigs = null;
                List<IReadOnlyVariable> newVariables = new();

                foreach (var cfg in currentVariableConfigs)
                {
                    if (variableManager.TryCreateVariable(cfg.Id, cfg.Name, cfg.Config, out var variable))
                    {
                        newVariables.Add(variable);
                    }
                    else
                    {
                        invalidVariableConfigs ??= new();
                        invalidVariableConfigs.Add(cfg);
                    }
                }

                InvalidVariableConfigs = invalidVariableConfigs;
                CurrentVariables = newVariables;
            }
            else
            {
                InvalidVariableConfigs = null;
                CurrentVariables = null;
            }
        }


        private event EventHandler<IVariableContext.VariableConfigsChangedEventArgs>? _currentVariableConfigsChanged;
        public event EventHandler<IVariableContext.VariableConfigsChangedEventArgs> CurrentVariableConfigsChanged
        {
            add
            {
                _currentVariableConfigsChanged += value;
            }
            remove
            {
                _currentVariableConfigsChanged -= value;
            }
        }

        private event EventHandler<IVariableContext.VariablesChangedEventArgs>? _currentVariablesChanged;
        public event EventHandler<IVariableContext.VariablesChangedEventArgs> CurrentVariablesChanged
        {
            add
            {
                _currentVariablesChanged += value;
            }
            remove
            {
                _currentVariablesChanged -= value;
            }
        }
    }
}
