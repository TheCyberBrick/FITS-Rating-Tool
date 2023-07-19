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

using Microsoft.VisualStudio.Threading;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.Progress;
using FitsRatingTool.GuiApp.UI.Progress.ViewModels;
using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.Common.Services;
using FitsRatingTool.GuiApp.UI.FitsImage;
using System.Reactive.Linq;
using DryIocAttributes;
using System.ComponentModel.Composition;
using FitsRatingTool.GuiApp.UI.Exporters;
using FitsRatingTool.IoC;

namespace FitsRatingTool.GuiApp.UI.Evaluation.ViewModels
{
    [Export(typeof(IEvaluationExportProgressViewModel)), TransientReuse]
    public class EvaluationExportProgressViewModel : SimpleProgressViewModel<ExportResult, EvaluationExportProgress>, IEvaluationExportProgressViewModel
    {
        public EvaluationExportProgressViewModel(IRegistrar<IEvaluationExportProgressViewModel, IEvaluationExportProgressViewModel.OfExporter> reg)
        {
            reg.RegisterAndReturn<EvaluationExportProgressViewModel>();
        }


        private class Context : EvaluationExporterContext
        {
            public override string ResolvePath(string path)
            {
                return Environment.ExpandEnvironmentVariables(path);
            }
        }


        private int _numberOfFiles;
        public int NumberOfFiles
        {
            get => _numberOfFiles;
            set => this.RaiseAndSetIfChanged(ref _numberOfFiles, value);
        }

        private int _currentFile;
        public int CurrentFile
        {
            get => _currentFile;
            set => this.RaiseAndSetIfChanged(ref _currentFile, value);
        }

        private string _currentFilePath = "";
        public string CurrentFilePath
        {
            get => _currentFilePath;
            set => this.RaiseAndSetIfChanged(ref _currentFilePath, value);
        }

        private string _currentFileName = "";
        public string CurrentFileName
        {
            get => _currentFileName;
            set => this.RaiseAndSetIfChanged(ref _currentFileName, value);
        }


        private float _currentProgressValue;
        public float ProgressValue
        {
            get => _currentProgressValue;
            set => this.RaiseAndSetIfChanged(ref _currentProgressValue, value);
        }

        public Interaction<ConfirmationEventArgs, ConfirmationEventArgs.Result> ExporterConfirmationDialog { get; } = new();



        private readonly List<CancellationTokenSource> loadingCts = new();

        private readonly string exporterId;
        private readonly IExporterConfiguratorViewModel exporterConfigurator;

        private readonly IFitsImageManager fitsImageManager;
        private readonly IEvaluationManager evaluationManager;
        private readonly IEvaluationService evaluationService;
        private readonly IInstrumentProfileManager instrumentProfileManager;

        private EvaluationExportProgressViewModel(
            IEvaluationExportProgressViewModel.OfExporter args, IFitsImageManager fitsImageManager,
            IEvaluationManager evaluationManager, IEvaluationService evaluationService,
            IInstrumentProfileManager instrumentProfileManager) : base(null)
        {
            this.exporterId = args.ExporterId;
            this.exporterConfigurator = args.ExporterConfigurator;
            this.fitsImageManager = fitsImageManager;
            this.evaluationManager = evaluationManager;
            this.evaluationService = evaluationService;
            this.instrumentProfileManager = instrumentProfileManager;
        }

        protected override Func<Task<Result<ExportResult>>> CreateTask(ProgressSynchronizationContext synchronizationContext)
        {
            return async () =>
            {
                using var cts = new CancellationTokenSource();

                loadingCts.Add(cts);

                try
                {
                    CancellationToken ct;
                    try
                    {
                        ct = cts.Token;
                    }
                    catch (ObjectDisposedException)
                    {
                        return CreateCancellation(new ExportResult(0, 0));
                    }

                    using var ctx = new Context();

                    using var exporter = exporterConfigurator.CreateExporter(ctx);

                    var confirmationMessage = exporter.ConfirmationMessage;
                    if (confirmationMessage != null)
                    {
                        var args = new ConfirmationEventArgs(exporterId, exporter, confirmationMessage);

                        ConfirmationEventArgs.Result result = ConfirmationEventArgs.Result.Proceed;

                        try
                        {
                            result = await ExporterConfirmationDialog.Handle(args);
                        }
                        catch (UnhandledInteractionException<ConfirmationEventArgs, ConfirmationEventArgs.Result>)
                        {
                            // OK
                        }

                        if (result == ConfirmationEventArgs.Result.Proceed)
                        {
                            result = await args.HandleAsync(ct);
                        }

                        if (result != ConfirmationEventArgs.Result.Proceed)
                        {
                            return CreateCancellation(new ExportResult("Export was aborted"));
                        }
                    }

                    var grouping = evaluationManager.CurrentGrouping;
                    var evaluationFormula = evaluationManager.CurrentFormula;

                    var variables = instrumentProfileManager.CurrentVariables;

                    if (!evaluationService.Build(evaluationFormula ?? "", variables, out var evaluator, out var formulaErrorMessage) || evaluator == null)
                    {
                        return CreateCancellation(new ExportResult("Invalid evaluation formula"));
                    }

                    var groups = new Dictionary<string, List<EvaluationItem>>();
                    var files = new Dictionary<IFitsImageStatisticsViewModel, string>();

                    int numFiles = fitsImageManager.Files.Count;

                    int numToExport = 0;

                    foreach (var file in fitsImageManager.Files)
                    {
                        if (IsCancelling)
                        {
                            break;
                        }

                        var record = fitsImageManager.Get(file);
                        if (record != null)
                        {
                            var stats = record.Statistics;
                            var metadata = record.Metadata;
                            if (stats != null && metadata != null)
                            {
                                var groupMatch = grouping != null ? grouping.GetGroupMatch(metadata) : null;
                                var groupKey = groupMatch != null ? groupMatch.GroupKey : "All";

                                IDictionary<string, Constant>? constants = null;

                                if (variables != null)
                                {
                                    Func<string, string?> headerMap = keyword =>
                                    {
                                        foreach (var record in metadata.Header)
                                        {
                                            if (record.Keyword == keyword)
                                            {
                                                return record.Value;
                                            }
                                        }
                                        return null;
                                    };

                                    constants = await evaluationService.EvaluateVariablesAsync(variables, file, headerMap);
                                }

                                if (!groups.TryGetValue(groupKey, out var group))
                                {
                                    groups.Add(groupKey, group = new());
                                }

                                group.Add(new EvaluationItem(stats, constants));
                                files.Add(stats, file);

                                ++numToExport;
                            }
                        }
                    }

                    int numExported = 0;

                    foreach (var pair in groups)
                    {
                        if (IsCancelling)
                        {
                            break;
                        }

                        var groupKey = pair.Key;
                        var statistics = pair.Value;

                        try
                        {
                            await evaluator.EvaluateAsync(statistics, 8, async (item, variableValues, value, ct) =>
                            {
                                var file = files[(IFitsImageStatisticsViewModel)item.Statistics];

                                ReportProgress(new EvaluationExportProgress
                                {
                                    numberOfFiles = numToExport,
                                    currentFile = numExported,
                                    currentFilePath = file
                                });

                                await exporter.ExportAsync(ctx, file, groupKey, variableValues, value, ct);
                                await exporter.FlushAsync(ct);

                                Interlocked.Increment(ref numExported);
                            }, null, ct);
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected
                        }
                    }

                    if (IsCancelling)
                    {
                        return CreateCancellation(new ExportResult(numFiles, numExported));
                    }
                    else
                    {
                        return CreateCompletion(new ExportResult(numFiles, numExported));
                    }
                }
                finally
                {
                    loadingCts.Remove(cts);
                }
            };
        }

        protected override void OnCancelling()
        {
            try
            {
                loadingCts.ForEach(c =>
                {
                    try
                    {
                        c.Cancel();
                    }
                    catch (Exception)
                    {
                    }
                });
            }
            catch (Exception)
            {
            }
        }

        protected override void OnProgressChanged(EvaluationExportProgress value)
        {
            NumberOfFiles = Math.Max(NumberOfFiles, value.numberOfFiles);
            if (value.currentFile >= CurrentFile)
            {
                CurrentFile = value.currentFile + 1;
                CurrentFilePath = value.currentFilePath;
                CurrentFileName = Path.GetFileName(value.currentFilePath);
                ProgressValue = Math.Max(ProgressValue, value.currentFile / (float)value.numberOfFiles);
            }
        }
    }
}
