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

using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.Common.Services;
using FitsRatingTool.Common.Utils;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Progress;
using FitsRatingTool.GuiApp.UI.Progress.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.JobRunner.ViewModels
{
    public class JobRunnerProgressViewModel : SimpleProgressViewModel<JobResult, BatchEvaluationProgressTracker.ProgressState>, IJobRunnerProgressViewModel
    {
        public class Factory : IJobRunnerProgressViewModel.IFactory
        {
            private readonly IStandaloneEvaluationService standaloneEvaluationService;
            private readonly IExporterConfiguratorManager exporterConfiguratorManager;

            public Factory(IStandaloneEvaluationService standaloneEvaluationService, IExporterConfiguratorManager exporterConfiguratorManager)
            {
                this.standaloneEvaluationService = standaloneEvaluationService;
                this.exporterConfiguratorManager = exporterConfiguratorManager;
            }

            public IJobRunnerProgressViewModel Create(string jobConfigFile, string path)
            {
                return new JobRunnerProgressViewModel(jobConfigFile, path, standaloneEvaluationService, exporterConfiguratorManager);
            }
        }

        private int _numberOfFiles;
        public int NumberOfFiles
        {
            get => _numberOfFiles;
            set => this.RaiseAndSetIfChanged(ref _numberOfFiles, value);
        }

        private int _loadedFiles;
        public int LoadedFiles
        {
            get => _loadedFiles;
            set => this.RaiseAndSetIfChanged(ref _loadedFiles, value);
        }

        private bool _isLoading = true;
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        private bool _isEvaluating;
        public bool IsEvaluating
        {
            get => _isEvaluating;
            set => this.RaiseAndSetIfChanged(ref _isEvaluating, value);
        }

        private float _progressValue;
        public float ProgressValue
        {
            get => _progressValue;
            set => this.RaiseAndSetIfChanged(ref _progressValue, value);
        }

        private float _speedValue;
        public float SpeedValue
        {
            get => _speedValue;
            set => this.RaiseAndSetIfChanged(ref _speedValue, value);
        }

        public Interaction<ConfirmationEventArgs, ConfirmationEventArgs.Result> ExporterConfirmationDialog { get; } = new();


        private readonly List<CancellationTokenSource> loadingCts = new();

        private readonly string jobConfigFile;
        private readonly string path;

        private readonly IStandaloneEvaluationService standaloneEvaluationService;

        private JobRunnerProgressViewModel(string jobConfigFile, string path, IStandaloneEvaluationService standaloneEvaluationService, IExporterConfiguratorManager exporterConfiguratorManager)
        {
            this.jobConfigFile = jobConfigFile;
            this.path = path;
            this.standaloneEvaluationService = standaloneEvaluationService;

            foreach (var pair in exporterConfiguratorManager.Factories)
            {
                standaloneEvaluationService.RegisterExporter(pair.Key, (ctx, config) =>
                {
                    var configurator = pair.Value.CreateConfigurator();

                    if (configurator.TryLoadConfig(config))
                    {
                        return configurator.CreateExporter(ctx);
                    }

                    throw new InvalidOperationException("Failed loading exporter config");
                });
            }
        }


        protected override Func<Task<Result<JobResult>>> CreateTask(ProgressSynchronizationContext synchronizationContext)
        {
            return async () =>
            {
                int numFiles = 0;
                int exportedFiles = 0;
                string? logFile = null;

                using var cts = new CancellationTokenSource();

                var ct = cts.Token;

                try
                {
                    BatchEvaluationProgressTracker? tracker = null;

                    using CancellationTokenSource progressTrackerCts = new();
                    try
                    {
                        using (ct.Register(() => progressTrackerCts.Cancel()))
                        {
                            loadingCts.Add(cts);

                            Regex? filePatternRegex = null;

                            Regex extensionPattern = new("\\.fit|\\.fits|\\.fts", RegexOptions.IgnoreCase);

                            List<string> files;

                            try
                            {
                                files = await Task.Run(() =>
                                {
                                    List<string> foundFiles = new();

                                    if (Directory.Exists(path))
                                    {
                                        try
                                        {
                                            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                                                .Where(file => extensionPattern.IsMatch(Path.GetExtension(file) ?? "") && (filePatternRegex == null || filePatternRegex.IsMatch(file))))
                                            {
                                                foundFiles.Add(file);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new Exception("Could not read files in directory '" + path + "'", ex);
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Could not find directory '" + path + "'");
                                    }

                                    return foundFiles;
                                });
                            }
                            catch (Exception ex)
                            {
                                return CreateCancellation(new JobResult(ex.Message, ex));
                            }

                            numFiles = files.Count;

                            CancellationToken progressTrackerCt = progressTrackerCts.Token;

                            tracker = BatchEvaluationProgressTracker.Track(numFiles, out var progressTrackerTask, 250, progressTrackerCt);
                            tracker.ProgressChanged += (s, state) => ReportProgress(state);

                            void onExporterConfirmation(object? sender, ConfirmationEventArgs e)
                            {
                                e.RegisterHandler(async ct =>
                                {
                                    try
                                    {
                                        return await ExporterConfirmationDialog.Handle(e);
                                    }
                                    catch (UnhandledInteractionException<ConfirmationEventArgs, ConfirmationEventArgs.Result>)
                                    {
                                        return ConfirmationEventArgs.Result.Proceed;
                                    }
                                });
                            }

                            standaloneEvaluationService.OnExporterConfirmation += onExporterConfirmation;

                            try
                            {
                                await standaloneEvaluationService.EvaluateAsync(jobConfigFile, files, (_, _, _, _, _) =>
                                {
                                    Interlocked.Increment(ref exportedFiles);
                                    return Task.CompletedTask;
                                },
                                e => tracker.OnEvent(e), lf => logFile = lf, ct);
                            }
                            catch (OperationCanceledException)
                            {
                                return CreateCancellation(new JobResult(numFiles, tracker.FilesLoaded, exportedFiles, logFile));
                            }
                            catch (Exception ex)
                            {
                                return CreateCancellation(new JobResult(ex));
                            }
                            finally
                            {
                                standaloneEvaluationService.OnExporterConfirmation -= onExporterConfirmation;
                            }

                            progressTrackerCts.Cancel();

                            await progressTrackerTask;
                        }
                    }
                    finally
                    {
                        progressTrackerCts.Cancel();
                    }

                    return CreateCompletion(new JobResult(numFiles, tracker.FilesLoaded, exportedFiles, logFile));
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

        protected override void OnProgressChanged(BatchEvaluationProgressTracker.ProgressState value)
        {
            NumberOfFiles = Math.Max(NumberOfFiles, value.FilesTotal);
            LoadedFiles = Math.Max(LoadedFiles, value.FilesLoaded);

            SpeedValue = (float)value.PhaseSpeedEstimate;

            if (value.FilesLoaded < value.FilesTotal)
            {
                ProgressValue = value.PhaseProgress / (float)value.FilesTotal;
                IsLoading = true;
                IsEvaluating = false;
            }
            else
            {
                ProgressValue = value.PhaseProgress / 100.0f;
                IsLoading = false;
                IsEvaluating = true;
            }
        }
    }
}
