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
using FitsRatingTool.Common.Services;
using FitsRatingTool.Common.Services.Impl;
using FitsRatingTool.Exporters.Services;
using FitsRatingTool.Exporters.Services.Impl;

namespace FitsRatingTool.ConsoleApp
{
    public class Bootstrapper
    {
        public static Container Initialize()
        {
            var container = new Container();

            container.Register<ConsoleApp, ConsoleApp>(Reuse.Singleton);

            RegisterServices(container);
            RegisterExporters(container);

            return container;
        }

        private static void RegisterServices(Container container)
        {
            container.Register<IFitsImageLoader, FitsImageLoader>(Reuse.Singleton);
            container.Register<IGroupingManager, GroupingManager>(Reuse.Singleton);
            container.Register<IEvaluationService, EvaluationService>(Reuse.Singleton);
            container.Register<IJobConfigFactory, JobConfigFactory>(Reuse.Singleton);
            container.Register<IBatchEvaluationService, BatchEvaluationService>(Reuse.Singleton);
            container.Register<IStandaloneEvaluationService, StandaloneEvaluationService>(Reuse.Singleton);
            container.Register<IInstrumentProfileFactory, InstrumentProfileFactory>(Reuse.Singleton);
        }

        private static void RegisterExporters(Container container)
        {
            container.Register<ICSVEvaluationExporterFactory, CSVEvaluationExporterFactory>(Reuse.Singleton);
            container.Register<IFitsHeaderEvaluationExporterFactory, FitsHeaderEvaluationExporterFactory>(Reuse.Singleton);
            container.Register<IVoyagerEvaluationExporterFactory, VoyagerEvaluationExporterFactory>(Reuse.Singleton);
            container.Register<IFileDeleterExporterFactory, FileDeleterExporterFactory>(Reuse.Singleton);
            container.Register<IFileMoverExporterFactory, FileMoverExporterFactory>(Reuse.Singleton);
        }
    }
}
