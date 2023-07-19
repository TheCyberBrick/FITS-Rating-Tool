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

using DryIoc;
using DryIoc.MefAttributedModel;
using FitsRatingTool.Common.Services;
using FitsRatingTool.Common.Services.Impl;
using FitsRatingTool.Exporters.Services;
using FitsRatingTool.Exporters.Services.Impl;
using FitsRatingTool.GuiApp.Services.Impl;
using FitsRatingTool.IoC;
using FitsRatingTool.IoC.Impl;

namespace FitsRatingTool.GuiApp
{
    public class Bootstrapper
    {
        public static IContainer Initialize()
        {
            // NB: The MEF model changes default reuse to transient
            var container = new Container(
                    rules => rules
                    .With(FactoryMethod.ConstructorWithResolvableArguments)
                    .WithAutoConcreteTypeResolution()
                ).WithMefAttributedModel();

            // Common
            container.Register<IFitsImageLoader, AppFitsImageLoader>(Reuse.Singleton);
            container.Register<IGroupingManager, GroupingManager>(Reuse.Singleton);
            container.Register<IEvaluationService, EvaluationService>(Reuse.Singleton);
            container.Register<IJobConfigFactory, JobConfigFactory>(Reuse.Singleton);
            container.Register<IBatchEvaluationService, BatchEvaluationService>(Reuse.Singleton);
            container.Register<IStandaloneEvaluationService, StandaloneEvaluationService>(Reuse.Transient);
            container.Register<IInstrumentProfileFactory, InstrumentProfileFactory>(Reuse.Singleton);
            container.Register<IVariableManager, VariableManager>(Reuse.Singleton);
            container.Register<IEvaluationExporterManager, EvaluationExporterManager>(Reuse.Singleton);

            // IoC
            container.RegisterExports(typeof(IContainer<,>).Assembly);
            container.Register(typeof(IContainerResolver), typeof(DryIoCContainerResolver), Reuse.Transient);

            // Exporters
            container.Register<ICSVEvaluationExporterFactory, CSVEvaluationExporterFactory>(Reuse.Singleton);
            container.Register<IFitsHeaderEvaluationExporterFactory, FitsHeaderEvaluationExporterFactory>(Reuse.Singleton);
            container.Register<IVoyagerEvaluationExporterFactory, VoyagerEvaluationExporterFactory>(Reuse.Singleton);
            container.Register<IFileDeleterExporterFactory, FileDeleterExporterFactory>(Reuse.Singleton);
            container.Register<IFileMoverExporterFactory, FileMoverExporterFactory>(Reuse.Singleton);

            // Scan and register MEF exports
            container.RegisterExports(typeof(Bootstrapper).Assembly);

            return container;
        }
    }
}
