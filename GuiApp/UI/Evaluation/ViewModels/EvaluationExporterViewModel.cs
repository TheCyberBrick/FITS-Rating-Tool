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
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Evaluation.ViewModels
{
    public class EvaluationExporterViewModel : IEvaluationExporterViewModel
    {
        public EvaluationExporterViewModel(IRegistrar<IEvaluationExporterViewModel, IEvaluationExporterViewModel.Of> reg)
        {
            reg.RegisterAndReturn<EvaluationExporterViewModel>();
        }


        public IEvaluationExporterConfiguratorViewModel EvaluationExporterConfigurator { get; private set; } = null!;

        public ReactiveCommand<Unit, IEvaluationExportProgressViewModel> ExportWithProgress { get; }

        public ReactiveCommand<Unit, Unit> ExportWithProgressDialog { get; }

        public Interaction<IEvaluationExportProgressViewModel, ExportResult> ExportProgressDialog { get; } = new();

        public Interaction<ExportResult, Unit> ExportResultDialog { get; } = new();



        private EvaluationExporterViewModel(IEvaluationExporterViewModel.Of args,
            IContainer<IEvaluationExporterConfiguratorViewModel, IEvaluationExporterConfiguratorViewModel.Of> evaluationExporterConfiguratorContainer,
            IJobConfigFactory jobConfigFactory,
            IContainer<IEvaluationExportProgressViewModel, IEvaluationExportProgressViewModel.OfExporter> evaluationExportProgressContainer)
        {
            var canExport = Observable.CombineLatest(
                this.WhenAnyValue(x => x.EvaluationExporterConfigurator.ExporterConfigurator),
                this.WhenAnyValue(x => x.EvaluationExporterConfigurator.ExporterConfigurator!.IsValid).Prepend(false).DefaultIfEmpty(false),
                (a, b) => a != null && b);

            evaluationExporterConfiguratorContainer.ToSingleton().Inject(new IEvaluationExporterConfiguratorViewModel.Of(), vm => EvaluationExporterConfigurator = vm);

            evaluationExportProgressContainer.ToSingleton();

            ExportWithProgress = ReactiveCommand.Create(() =>
            {
                var id = EvaluationExporterConfigurator.SelectedExporterConfiguratorFactory?.Id;
                var configurator = EvaluationExporterConfigurator.ExporterConfigurator;
                if (id != null && configurator != null && configurator.IsValid)
                {
                    return evaluationExportProgressContainer.Instantiate(new IEvaluationExportProgressViewModel.OfExporter(id, configurator));
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
