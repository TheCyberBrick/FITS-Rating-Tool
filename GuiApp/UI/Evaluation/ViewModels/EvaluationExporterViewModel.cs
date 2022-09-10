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

using FitsRatingTool.Common.Services;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Evaluation.ViewModels
{
    public class EvaluationExporterViewModel : IEvaluationExporterViewModel
    {
        public class Factory : IEvaluationExporterViewModel.IFactory
        {
            private readonly IEvaluationExporterConfiguratorViewModel.IFactory evaluationExporterConfiguratorFactory;
            private readonly IJobConfigFactory jobConfigFactory;
            private readonly IEvaluationExportProgressViewModel.IFactory evaluationExportProgressFactory;

            public Factory(IEvaluationExporterConfiguratorViewModel.IFactory evaluationExporterConfiguratorFactory, IJobConfigFactory jobConfigFactory, IEvaluationExportProgressViewModel.IFactory evaluationExportProgressFactory)
            {
                this.evaluationExporterConfiguratorFactory = evaluationExporterConfiguratorFactory;
                this.jobConfigFactory = jobConfigFactory;
                this.evaluationExportProgressFactory = evaluationExportProgressFactory;
            }

            public IEvaluationExporterViewModel Create()
            {
                return new EvaluationExporterViewModel(evaluationExporterConfiguratorFactory, jobConfigFactory, evaluationExportProgressFactory);
            }
        }


        public IEvaluationExporterConfiguratorViewModel EvaluationExporterConfigurator { get; }

        public ReactiveCommand<Unit, IEvaluationExportProgressViewModel> ExportWithProgress { get; }

        public ReactiveCommand<Unit, Unit> ExportWithProgressDialog { get; }

        public Interaction<IEvaluationExportProgressViewModel, ExportResult> ExportProgressDialog { get; } = new();

        public Interaction<ExportResult, Unit> ExportResultDialog { get; } = new();



        private EvaluationExporterViewModel(IEvaluationExporterConfiguratorViewModel.IFactory evaluationExporterConfiguratorFactory, IJobConfigFactory jobConfigFactory, IEvaluationExportProgressViewModel.IFactory evaluationExportProgressFactory)
        {
            var canExport = Observable.CombineLatest(
                this.WhenAnyValue(x => x.EvaluationExporterConfigurator.ExporterConfigurator),
                this.WhenAnyValue(x => x.EvaluationExporterConfigurator.ExporterConfigurator!.IsValid).Prepend(false).DefaultIfEmpty(false),
                (a, b) => a != null && b);

            EvaluationExporterConfigurator = evaluationExporterConfiguratorFactory.Create();

            ExportWithProgress = ReactiveCommand.Create(() =>
            {
                var id = EvaluationExporterConfigurator.SelectedExporterConfiguratorFactory?.Id;
                var configurator = EvaluationExporterConfigurator.ExporterConfigurator;
                if (id != null && configurator != null && configurator.IsValid)
                {
                    return evaluationExportProgressFactory.Create(id, configurator);
                }

                throw new InvalidOperationException("Configurator is null or invalid");
            }, canExport);

            ExportWithProgressDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var vm = await ExportWithProgress.Execute();
                if (vm != null)
                {
                    var result = await ExportProgressDialog.Handle(vm);

                    if (result.Error != null)
                    {
                        Debug.WriteLine(result.Error.Message);
                        Debug.WriteLine(result.Error.StackTrace);
                    }

                    try
                    {
                        await ExportResultDialog.Handle(result);
                    }
                    catch (UnhandledInteractionException<ExportResult, Unit>)
                    {
                        // OK, result dialog is optional
                    }
                }
            }, canExport);
        }
    }
}
