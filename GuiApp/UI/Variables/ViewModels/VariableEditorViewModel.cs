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
using FitsRatingTool.IoC;
using ReactiveUI;
using System;
using System.ComponentModel.Composition;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Variables.ViewModels
{
    [Export(typeof(IVariableEditorViewModel)), TransientReuse, AllowDisposableTransient]
    public class VariableEditorViewModel : ViewModelBase, IVariableEditorViewModel, IDisposable
    {
        public VariableEditorViewModel(IRegistrar<IVariableEditorViewModel, IVariableEditorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<VariableEditorViewModel>();
        }


        public IVariableSelectorViewModel Selector { get; private set; } = null!;

        private IDisposable? _configuratorDisposable;
        private IVariableConfiguratorViewModel? _configurator;
        public IVariableConfiguratorViewModel? VariableConfigurator
        {
            get => _configurator;
            private set => this.RaiseAndSetIfChanged(ref _configurator, value);
        }


        private EventHandler? _configurationChanged;
        public event EventHandler ConfigurationChanged
        {
            add => _configurationChanged += value;
            remove => _configurationChanged -= value;
        }


        private IComponentRegistry<IVariableConfiguratorViewModel> variableConfiguratorRegistry = null!;

        private VariableEditorViewModel(IVariableEditorViewModel.Of args,
            IContainer<IComponentRegistry<IVariableConfiguratorViewModel>, IComponentRegistry<IVariableConfiguratorViewModel>.Of> variableConfiguratorRegistryContainer)
        {
            variableConfiguratorRegistryContainer.Inject(new(), this, x => x.variableConfiguratorRegistry);

            this.WhenAnyValue(x => x.Selector.SelectedVariable)
                .Skip(1)
                .Subscribe(x => ReplaceConfigurator(x?.Id));
        }

        private void ReplaceConfigurator(string? id)
        {
            ReplaceConfigurator(variableConfiguratorRegistry.GetFactory(id));
        }

        private void ReplaceConfigurator(IDelegatedFactory<IVariableConfiguratorViewModel>? factory)
        {
            IDisposable? newConfiguratorDisposable = null;
            IVariableConfiguratorViewModel? newConfigurator = factory != null ? factory.Instantiate(out newConfiguratorDisposable) : null;

            var oldConfigurator = VariableConfigurator;

            if (oldConfigurator != null)
            {
                if (newConfigurator != null && oldConfigurator.GetType() == newConfigurator.GetType())
                {
                    // Same configurator, no changes needed
                    newConfiguratorDisposable?.Dispose();
                    return;
                }

                UnsubscribeFromEvent<IVariableConfiguratorViewModel, EventArgs, VariableEditorViewModel>(oldConfigurator, nameof(oldConfigurator.ConfigurationChanged), OnConfigurationChanged);
            }

            var oldExporterConfiguratorDisposable = _configuratorDisposable;

            _configuratorDisposable = newConfiguratorDisposable;
            VariableConfigurator = newConfigurator;

            oldExporterConfiguratorDisposable?.Dispose();

            if (newConfigurator != null)
            {
                SubscribeToEvent<IVariableConfiguratorViewModel, EventArgs, VariableEditorViewModel>(newConfigurator, nameof(newConfigurator.ConfigurationChanged), OnConfigurationChanged);
            }

            _configurationChanged?.Invoke(this, new EventArgs());
        }

        private void OnConfigurationChanged(object? sender, EventArgs e)
        {
            if (sender == VariableConfigurator)
            {
                _configurationChanged?.Invoke(this, new EventArgs());
            }
        }

        public void SetVariableConfigurator(IDelegatedFactory<IVariableConfiguratorViewModel>? delegatedFactory)
        {
            ReplaceConfigurator(delegatedFactory); // Set configurator first so it keeps its loaded data

            if (delegatedFactory != null)
            {
                foreach (var id in variableConfiguratorRegistry.Ids)
                {
                    var registration = variableConfiguratorRegistry.GetRegistration(id);
                    var factory = variableConfiguratorRegistry.GetFactory(id);

                    if (registration != null && factory != null && factory.InstanceType == delegatedFactory.InstanceType)
                    {
                        if (Selector.SelectById(registration.Id) != null)
                        {
                            return;
                        }
                    }
                }
                throw new InvalidOperationException("Tried setting exporter configurator to unknown type '" + delegatedFactory.InstanceType.FullName + "'");
            }
            else
            {
                Selector.SelectedVariable = null;
            }
        }

        public void Dispose()
        {
            _configuratorDisposable?.Dispose();
            _configuratorDisposable = null;
        }
    }
}
