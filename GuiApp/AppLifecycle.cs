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
            reg.RegisterAndReturn<AppLifecycle>();
        }


        private readonly IAppConfigManager appConfigManager;
        private readonly IInstrumentProfileRepository instrumentProfileRepository;
        private readonly IInstrumentProfileManager instrumentProfileManager;
        private readonly IEvaluationExporterManager evaluationExporterManager;
        private readonly IVariableManager variableManager;

        private readonly IContainerRoot<IAppViewModel, IAppViewModel.Of> appRoot;

        private AppLifecycle(
            IAppLifecycle.Of args,
            IAppConfigManager appConfigManager,
            IInstrumentProfileRepository instrumentProfileRepository,
            IInstrumentProfileManager instrumentProfileManager,
            IEvaluationExporterManager evaluationExporterManager,
            IVariableManager variableManager,
            IContainer<IComponentRegistry<IExporterConfiguratorViewModel>, IComponentRegistry<IExporterConfiguratorViewModel>.Of> exporterConfiguratorRegistryContainer,
            IContainer<IComponentRegistry<IVariableConfiguratorViewModel>, IComponentRegistry<IVariableConfiguratorViewModel>.Of> variableConfiguratorRegistryContainer,
            IContainerRoot<IAppViewModel, IAppViewModel.Of> appRoot)
        {
            this.appConfigManager = appConfigManager;
            this.instrumentProfileRepository = instrumentProfileRepository;
            this.instrumentProfileManager = instrumentProfileManager;
            this.evaluationExporterManager = evaluationExporterManager;
            this.variableManager = variableManager;

            this.appRoot = appRoot;

            exporterConfiguratorRegistryContainer.Singleton().Inject(new IComponentRegistry<IExporterConfiguratorViewModel>.Of(), RegisterExporters);
            variableConfiguratorRegistryContainer.Singleton().Inject(new IComponentRegistry<IVariableConfiguratorViewModel>.Of(), RegisterVariables);
        }

        public void OnInstantiated()
        {
            appConfigManager.Load();
            instrumentProfileRepository.Load();
            instrumentProfileManager.Load();
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
            var disposable = appRoot.Instantiate(new IAppViewModel.Of(), out IAppViewModel vm, false /* TODO This should be true but currently causes an error */);
            dataContext = vm;
            return disposable;
        }
    }
}
