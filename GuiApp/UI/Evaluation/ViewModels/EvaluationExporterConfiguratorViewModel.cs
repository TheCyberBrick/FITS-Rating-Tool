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
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using static FitsRatingTool.GuiApp.UI.Evaluation.IEvaluationExporterConfiguratorViewModel;

namespace FitsRatingTool.GuiApp.UI.Evaluation.ViewModels
{
    public class EvaluationExporterConfiguratorViewModel : ViewModelBase, IEvaluationExporterConfiguratorViewModel
    {
        public class Factory : IFactory
        {
            private readonly IExporterConfiguratorManager exporterConfiguratorManager;

            public Factory(IExporterConfiguratorManager exporterConfiguratorManager)
            {
                this.exporterConfiguratorManager = exporterConfiguratorManager;
            }

            public IEvaluationExporterConfiguratorViewModel Create()
            {
                return new EvaluationExporterConfiguratorViewModel(exporterConfiguratorManager);
            }
        }


        public IReadOnlyList<ExporterConfiguratorFactory> ExporterConfiguratorFactories { get; }

        private ExporterConfiguratorFactory? _exporterConfiguratorFactory;
        public ExporterConfiguratorFactory? SelectedExporterConfiguratorFactory
        {
            get => _exporterConfiguratorFactory;
            set => this.RaiseAndSetIfChanged(ref _exporterConfiguratorFactory, value);
        }

        private IExporterConfiguratorManager.IExporterConfiguratorViewModel? _exporterConfigurator;
        public IExporterConfiguratorManager.IExporterConfiguratorViewModel? ExporterConfigurator
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


        private EvaluationExporterConfiguratorViewModel(IExporterConfiguratorManager exporterConfiguratorManager)
        {
            var exporterConfiguratorFactories = new List<ExporterConfiguratorFactory>();
            foreach (var pair in exporterConfiguratorManager.Factories)
            {
                exporterConfiguratorFactories.Add(new ExporterConfiguratorFactory(pair.Key, pair.Value));
            }
            ExporterConfiguratorFactories = exporterConfiguratorFactories;

            this.WhenAnyValue(x => x.SelectedExporterConfiguratorFactory)
                .Skip(1)
                .Subscribe(x =>
                {
                    var newConfigurator = x != null ? x.Factory.CreateConfigurator() : null;
                    ReplaceExporterConfigurator(newConfigurator);
                });
        }

        private void ReplaceExporterConfigurator(IExporterConfiguratorManager.IExporterConfiguratorViewModel? newConfigurator)
        {
            var oldConfigurator = ExporterConfigurator;

            if (oldConfigurator != null)
            {
                if (newConfigurator != null && oldConfigurator.GetType() == newConfigurator.GetType())
                {
                    // Same configurator, no changes needed
                    return;
                }

                WeakEventHandlerManager.Unsubscribe<EventArgs, EvaluationExporterConfiguratorViewModel>(oldConfigurator, nameof(oldConfigurator.ConfigurationChanged), OnExporterConfigurationChanged);
            }

            ExporterConfigurator = newConfigurator;

            if (newConfigurator != null)
            {
                WeakEventHandlerManager.Subscribe<IExporterConfiguratorManager.IExporterConfiguratorViewModel, EventArgs, EvaluationExporterConfiguratorViewModel>(newConfigurator, nameof(newConfigurator.ConfigurationChanged), OnExporterConfigurationChanged);
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

        public void SetExporterConfigurator(IExporterConfiguratorManager.IExporterConfiguratorViewModel? exporterConfigurator)
        {
            ReplaceExporterConfigurator(exporterConfigurator); // Set configurator first so it keeps its loaded data

            if (exporterConfigurator != null)
            {
                foreach (var factory in ExporterConfiguratorFactories)
                {
                    if (factory.Factory.CreateConfigurator().GetType() == exporterConfigurator.GetType())
                    {
                        SelectedExporterConfiguratorFactory = factory;
                        return;
                    }
                }
                throw new InvalidOperationException("Tried setting exporter configurator to unknown type '" + exporterConfigurator.GetType().FullName + "'");
            }
            else
            {
                SelectedExporterConfiguratorFactory = null;
            }
        }
    }
}
