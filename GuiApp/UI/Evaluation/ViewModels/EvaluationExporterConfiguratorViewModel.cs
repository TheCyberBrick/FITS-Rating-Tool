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
using FitsRatingTool.GuiApp.UI.Exporters;
using FitsRatingTool.IoC;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reactive.Linq;
using static FitsRatingTool.GuiApp.UI.Evaluation.IEvaluationExporterConfiguratorViewModel;

namespace FitsRatingTool.GuiApp.UI.Evaluation.ViewModels
{
    [Export(typeof(IEvaluationExporterConfiguratorViewModel)), TransientReuse, AllowDisposableTransient]
    public class EvaluationExporterConfiguratorViewModel : ViewModelBase, IEvaluationExporterConfiguratorViewModel, IDisposable
    {
        public EvaluationExporterConfiguratorViewModel(IRegistrar<IEvaluationExporterConfiguratorViewModel, IEvaluationExporterConfiguratorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<EvaluationExporterConfiguratorViewModel>();
        }


        public IReadOnlyList<ExporterConfiguratorFactory> ExporterConfiguratorFactories { get; private set; } = null!;

        private ExporterConfiguratorFactory? _exporterConfiguratorFactory;
        public ExporterConfiguratorFactory? SelectedExporterConfiguratorFactory
        {
            get => _exporterConfiguratorFactory;
            set => this.RaiseAndSetIfChanged(ref _exporterConfiguratorFactory, value);
        }

        private IDisposable? _exporterConfiguratorDisposable;
        private IExporterConfiguratorViewModel? _exporterConfigurator;
        public IExporterConfiguratorViewModel? ExporterConfigurator
        {
            get => _exporterConfigurator;
            private set => this.RaiseAndSetIfChanged(ref _exporterConfigurator, value);
        }


        private EventHandler? _configurationChanged;
        public event EventHandler ConfigurationChanged
        {
            add => _configurationChanged += value;
            remove => _configurationChanged -= value;
        }


        private EvaluationExporterConfiguratorViewModel(IEvaluationExporterConfiguratorViewModel.Of args,
            IContainer<IComponentRegistry<IExporterConfiguratorViewModel>, IComponentRegistry<IExporterConfiguratorViewModel>.Of> exporterConfiguratorRegistryContainer)
        {
            exporterConfiguratorRegistryContainer.ToSingleton().Inject(new IComponentRegistry<IExporterConfiguratorViewModel>.Of(), registry =>
            {
                var exporterConfiguratorFactories = new List<ExporterConfiguratorFactory>();

                foreach (var id in registry.Ids)
                {
                    var registration = registry.GetRegistration(id);
                    var factory = registry.GetFactory(id);

                    if (registration != null && factory != null)
                    {
                        exporterConfiguratorFactories.Add(new ExporterConfiguratorFactory(id, registration.Name, factory));
                    }
                }

                ExporterConfiguratorFactories = exporterConfiguratorFactories;
            });

            this.WhenAnyValue(x => x.SelectedExporterConfiguratorFactory)
                .Skip(1)
                .Subscribe(x => ReplaceExporterConfigurator(x?.Factory));
        }

        private void ReplaceExporterConfigurator(IDelegatedFactory<IExporterConfiguratorViewModel>? factory)
        {
            IDisposable? newConfiguratorDisposable = null;
            IExporterConfiguratorViewModel? newConfigurator = factory != null ? factory.Instantiate(out newConfiguratorDisposable) : null;

            var oldConfigurator = ExporterConfigurator;

            if (oldConfigurator != null)
            {
                if (newConfigurator != null && oldConfigurator.GetType() == newConfigurator.GetType())
                {
                    // Same configurator, no changes needed
                    newConfiguratorDisposable?.Dispose();
                    return;
                }

                UnsubscribeFromEvent<IExporterConfiguratorViewModel, EventArgs, EvaluationExporterConfiguratorViewModel>(oldConfigurator, nameof(oldConfigurator.ConfigurationChanged), OnExporterConfigurationChanged);
            }

            var oldExporterConfiguratorDisposable = _exporterConfiguratorDisposable;

            _exporterConfiguratorDisposable = newConfiguratorDisposable;
            ExporterConfigurator = newConfigurator;

            oldExporterConfiguratorDisposable?.Dispose();

            if (newConfigurator != null)
            {
                SubscribeToEvent<IExporterConfiguratorViewModel, EventArgs, EvaluationExporterConfiguratorViewModel>(newConfigurator, nameof(newConfigurator.ConfigurationChanged), OnExporterConfigurationChanged);
            }

            _configurationChanged?.Invoke(this, new EventArgs());
        }

        private void OnExporterConfigurationChanged(object? sender, EventArgs e)
        {
            if (sender == ExporterConfigurator)
            {
                _configurationChanged?.Invoke(this, new EventArgs());
            }
        }

        public void SetExporterConfigurator(IDelegatedFactory<IExporterConfiguratorViewModel>? delegatedFactory)
        {
            ReplaceExporterConfigurator(delegatedFactory); // Set configurator first so it keeps its loaded data

            if (delegatedFactory != null)
            {
                foreach (var factory in ExporterConfiguratorFactories)
                {
                    if (factory.Factory.InstanceType == delegatedFactory.InstanceType)
                    {
                        SelectedExporterConfiguratorFactory = factory;
                        return;
                    }
                }
                throw new InvalidOperationException("Tried setting exporter configurator to unknown type '" + delegatedFactory.InstanceType.FullName + "'");
            }
            else
            {
                SelectedExporterConfiguratorFactory = null;
            }
        }

        public void Dispose()
        {
            _exporterConfiguratorDisposable?.Dispose();
            _exporterConfiguratorDisposable = null;
        }
    }
}
