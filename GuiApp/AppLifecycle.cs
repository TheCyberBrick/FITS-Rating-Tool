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
using DryIoc;
using DryIocAttributes;
using FitsRatingTool.Common.Services;
using FitsRatingTool.GuiApp.Repositories;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.App;
using FitsRatingTool.GuiApp.UI.Exporters;
using FitsRatingTool.GuiApp.UI.Variables;
using FitsRatingTool.IoC;
using System;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp
{
    [Export(typeof(IAppLifecycle)), TransientReuse]
    public class AppLifecycle : IAppLifecycle, ILifecycleSubscriber
    {
        public AppLifecycle(IRegistrar<IAppLifecycle, IAppLifecycle.Of> reg)
        {
            reg.WithScopes(
                AppScopes.Service.Windowing,
                AppScopes.Context.Evaluation,
                AppScopes.Context.Variable,
                AppScopes.Context.InstrumentProfile,
                AppScopes.Context.ImageSelection
                ).RegisterAndReturn<AppLifecycle>();
        }


        public IResolverContext Resolver { get; }

        public IRegistrator Registrator { get; }


        private readonly IAppConfigManager appConfigManager;

        private readonly IInstrumentProfileRepository instrumentProfileRepository;
        private readonly IInstrumentProfileManager instrumentProfileManager;
        private readonly IInstrumentProfileContext instrumentProfileContext;

        private readonly IEvaluationManager evaluationManager;
        private readonly IEvaluationContext evaluationContext;
        private readonly IEvaluationExporterManager evaluationExporterManager;

        private readonly IVariableManager variableManager;
        private readonly IVariableContext variableContext;

        private readonly IContainerRoot<IAppViewModel, IAppViewModel.Of> appRoot;

        private AppLifecycle(
            IAppLifecycle.Of args,
            IResolverContext resolver,
            IRegistrator registrator,
            IAppConfigManager appConfigManager,
            IInstrumentProfileRepository instrumentProfileRepository,
            IInstrumentProfileManager instrumentProfileManager,
            IInstrumentProfileContext instrumentProfileContext,
            IEvaluationManager evaluationManager,
            IEvaluationContext evaluationContext,
            IEvaluationExporterManager evaluationExporterManager,
            IVariableManager variableManager,
            IVariableContext variableContext,
            IContainerRoot<IAppViewModel, IAppViewModel.Of> appRoot,
            IContainer<IComponentRegistry<IExporterConfiguratorViewModel>, IComponentRegistry<IExporterConfiguratorViewModel>.Of> exporterConfiguratorRegistryContainer,
            IContainer<IComponentRegistry<IVariableConfiguratorViewModel>, IComponentRegistry<IVariableConfiguratorViewModel>.Of> variableConfiguratorRegistryContainer)
        {
            this.appConfigManager = appConfigManager;

            this.instrumentProfileRepository = instrumentProfileRepository;
            this.instrumentProfileManager = instrumentProfileManager;
            this.instrumentProfileContext = instrumentProfileContext;

            this.evaluationManager = evaluationManager;
            this.evaluationContext = evaluationContext;
            this.evaluationExporterManager = evaluationExporterManager;

            this.variableManager = variableManager;
            this.variableContext = variableContext;

            this.appRoot = appRoot;

            Resolver = resolver;
            Registrator = registrator;

            exporterConfiguratorRegistryContainer.Singleton().Inject(new IComponentRegistry<IExporterConfiguratorViewModel>.Of(), RegisterExporters);
            variableConfiguratorRegistryContainer.Singleton().Inject(new IComponentRegistry<IVariableConfiguratorViewModel>.Of(), RegisterVariables);
        }

        public void OnInstantiated()
        {
            SetupServices();
            SetupContext();
        }

        private void SetupServices()
        {
            appConfigManager.Load();
            instrumentProfileRepository.Load();
            instrumentProfileManager.Load();
        }

        private void SetupContext()
        {
            instrumentProfileContext.LoadFromConfig();

            variableContext.LoadFromCurrentProfile();

            evaluationContext.LoadFromCurrentProfile();

            evaluationManager.EvaluationContext = evaluationContext;
            evaluationManager.VariableContext = variableContext;

            WeakEventHandlerManager.Subscribe<IInstrumentProfileContext, IInstrumentProfileContext.ProfileChangedEventArgs, AppLifecycle>(instrumentProfileContext, nameof(instrumentProfileContext.CurrentProfileChanged), OnCurrentProfileChanged);
            WeakEventHandlerManager.Subscribe<IEvaluationContext, IEvaluationContext.FormulaChangedEventArgs, AppLifecycle>(evaluationContext, nameof(evaluationContext.CurrentFormulaChanged), OnCurrentFormulaChanged);
            WeakEventHandlerManager.Subscribe<IEvaluationContext, IEvaluationContext.GroupingConfigurationChangedEventArgs, AppLifecycle>(evaluationContext, nameof(evaluationContext.CurrentGroupingConfigurationChanged), OnCurrentGroupingConfigurationChanged);
        }

        public void OnDestroying()
        {
        }

        public void OnDestroyed()
        {
        }

        private void RegisterExporters(IComponentRegistry<IExporterConfiguratorViewModel> configuratorRegistry)
        {
            foreach (var id in configuratorRegistry.Ids)
            {
                var factory = configuratorRegistry.GetFactory(id);

                if (factory != null)
                {
                    evaluationExporterManager.Register(id, (ctx, config) =>
                    {
                        var exporter = factory.Do(configurator =>
                        {
                            if (configurator.TryLoadConfig(config))
                            {
                                return configurator.CreateExporter(ctx);
                            }
                            return null;
                        });

                        if (exporter != null)
                        {
                            return exporter;
                        }
                        else
                        {
                            throw new InvalidOperationException("Failed loading exporter config");
                        }
                    });
                }
            }
        }

        private void RegisterVariables(IComponentRegistry<IVariableConfiguratorViewModel> configuratorRegistry)
        {
            foreach (var id in configuratorRegistry.Ids)
            {
                var factory = configuratorRegistry.GetFactory(id);

                if (factory != null)
                {
                    variableManager.Register(id, (name, config) =>
                    {
                        var exporter = factory.Do(configurator =>
                        {
                            if (configurator.TryLoadConfig(name, config))
                            {
                                return configurator.CreateVariable();
                            }
                            return null;
                        });

                        if (exporter != null)
                        {
                            return exporter;
                        }
                        else
                        {
                            throw new InvalidOperationException("Failed loading variable config");
                        }
                    });
                }
            }
        }

        public IDisposable CreateRootDataContext(out object dataContext)
        {
            var disposable = appRoot.Instantiate(new IAppViewModel.Of(), out IAppViewModel vm, true);
            dataContext = vm;
            return disposable;
        }

        private void OnCurrentProfileChanged(object? sender, IInstrumentProfileContext.ProfileChangedEventArgs e)
        {
            // Keep current variables in sync with the currently
            // selected profile
            variableContext.LoadFromCurrentProfile(instrumentProfileContext);

            // If evaluation context data was loaded from previous
            // profile and has since not changed then it should
            // be updated
            if (evaluationContext.LoadedInstrumentProfile == e.OldProfile)
            {
                evaluationContext.LoadFromCurrentProfile();
            }

            // Image statistics need to be invalidated when
            // profile has changed because they have to be
            // reanalyzed
            evaluationManager.InvalidateStatistics();
        }

        private void OnCurrentFormulaChanged(object? sender, IEvaluationContext.FormulaChangedEventArgs args)
        {
            evaluationManager.InvalidateEvaluation();
        }

        private void OnCurrentGroupingConfigurationChanged(object? sender, IEvaluationContext.GroupingConfigurationChangedEventArgs args)
        {
            evaluationManager.InvalidateEvaluation();
        }
    }
}
