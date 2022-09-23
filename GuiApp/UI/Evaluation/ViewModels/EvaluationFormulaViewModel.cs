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
using Microsoft.VisualStudio.Threading;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.Utils;
using Avalonia.Utilities;
using Avalonia.Collections;
using FitsRatingTool.GuiApp.Models;

namespace FitsRatingTool.GuiApp.UI.Evaluation.ViewModels
{
    public class EvaluationFormulaViewModel : ViewModelBase, IEvaluationFormulaViewModel
    {
        public class Factory : IEvaluationFormulaViewModel.IFactory
        {
            private readonly IFitsImageManager manager;
            private readonly IEvaluationService evaluationService;
            private readonly IEvaluationManager evaluationManager;
            private readonly IJobGroupingConfiguratorViewModel.IFactory groupingConfiguratorFactory;
            private readonly IGroupingManager groupingManager;

            public Factory(IFitsImageManager manager, IEvaluationService evaluationService, IEvaluationManager evaluationManager,
                IJobGroupingConfiguratorViewModel.IFactory groupingConfiguratorFactory, IGroupingManager groupingManager)
            {
                this.manager = manager;
                this.evaluationService = evaluationService;
                this.evaluationManager = evaluationManager;
                this.groupingConfiguratorFactory = groupingConfiguratorFactory;
                this.groupingManager = groupingManager;
            }

            public IEvaluationFormulaViewModel Create()
            {
                return new EvaluationFormulaViewModel(manager, evaluationService, evaluationManager, groupingConfiguratorFactory, groupingManager);
            }
        }


        private string? _ratingFormula;
        public string? RatingFormula
        {
            get => _ratingFormula;
            set => this.RaiseAndSetIfChanged(ref _ratingFormula, value);
        }

        private bool _autoUpdateRatings;
        public bool AutoUpdateRatings
        {
            get => _autoUpdateRatings;
            set => this.RaiseAndSetIfChanged(ref _autoUpdateRatings, value);
        }

        public ReactiveCommand<Unit, Unit> UpdateRatings { get; }

        private string? _ratingFormulaError;
        public string? RatingFormulaError
        {
            get => _ratingFormulaError;
            set => this.RaiseAndSetIfChanged(ref _ratingFormulaError, value);
        }

        public Interaction<string, Unit> RatingFormulaErrorDialog { get; } = new();


        private IEvaluationService.IEvaluator? _evaluatorInstance;
        private IEvaluationService.IEvaluator? EvaluatorInstance
        {
            get => _evaluatorInstance;
            set => this.RaiseAndSetIfChanged(ref this._evaluatorInstance, value);
        }

        public bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set => this.RaiseAndSetIfChanged(ref _isModified, value);
        }

        public string? _loadedFile;
        public string? LoadedFile
        {
            get => _loadedFile;
            set => this.RaiseAndSetIfChanged(ref _loadedFile, value);
        }

        public ReactiveCommand<Unit, Unit> LoadFormulaWithOpenFileDialog { get; }

        public Interaction<Unit, string> LoadFormulaOpenFileDialog { get; } = new();

        public ReactiveCommand<Unit, Unit> SaveFormula { get; }

        public ReactiveCommand<Unit, Unit> SaveFormulaWithSaveFileDialog { get; }

        public Interaction<Unit, string> SaveFormulaSaveFileDialog { get; } = new();




        public AvaloniaList<string> GroupKeys { get; } = new() { "All" };

        public IJobGroupingConfiguratorViewModel GroupingConfigurator { get; }




        private readonly IFitsImageManager manager;
        private readonly IEvaluationService evaluationService;
        private readonly IEvaluationManager evaluationManager;
        private readonly IGroupingManager groupingManager;

        private EvaluationFormulaViewModel(IFitsImageManager manager, IEvaluationService evaluationService, IEvaluationManager evaluationManager,
            IJobGroupingConfiguratorViewModel.IFactory groupingConfiguratorFactory, IGroupingManager groupingManager)
        {
            this.manager = manager;
            this.evaluationService = evaluationService;
            this.evaluationManager = evaluationManager;
            this.groupingManager = groupingManager;

            AutoUpdateRatings = evaluationManager.AutoUpdateRatings;

            var defaultGroupingConfiguration = evaluationManager.CurrentGroupingConfiguration;
            if (defaultGroupingConfiguration == null)
            {
                // By default group by object and filter
                defaultGroupingConfiguration = new GroupingConfiguration(true, true, false, false, false, false, 0, null);
            }

            GroupingConfigurator = groupingConfiguratorFactory.Create(defaultGroupingConfiguration);
            evaluationManager.CurrentGroupingConfiguration = GroupingConfigurator.GroupingConfiguration;

            var currentFormula = evaluationManager.CurrentFormula;

            UpdateRatings = ReactiveCommand.CreateFromTask(UpdateRatingsAsync, this.WhenAnyValue(x => x.EvaluatorInstance).Select(x => x != null));

            this.WhenAnyValue(x => x.RatingFormula).Subscribe(x =>
            {
                EvaluatorInstance = null;
                IsModified = true;
            });

            var OnRatingFormulaChange = () => this.WhenAnyValue(x => x.RatingFormula)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler);
            OnRatingFormulaChange.Observe(UpdateEvaluatorInstanceAsync).WithExceptionHandler(ex =>
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex);
            }).Subscribe();

            this.WhenAnyValue(x => x.AutoUpdateRatings)
                .Skip(1)
                .Subscribe(x => evaluationManager.AutoUpdateRatings = x);

            // No skip because group keys should be updated on load
            this.WhenAnyValue(x => x.GroupingConfigurator.GroupingConfiguration)
                .Subscribe(x => UpdateGroupingConfiguration(x));

            RatingFormula = currentFormula;

            LoadFormulaWithOpenFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var file = await LoadFormulaOpenFileDialog.Handle(Unit.Default);
                if (file.Length > 0)
                {
                    RatingFormula = await File.ReadAllTextAsync(file);
                    LoadedFile = file;
                    IsModified = false;
                }
            });

            SaveFormulaWithSaveFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var file = await SaveFormulaSaveFileDialog.Handle(Unit.Default);
                if (file.Length > 0)
                {
                    await File.WriteAllTextAsync(file, RatingFormula);
                    LoadedFile = file;
                    IsModified = false;
                }
            });

            SaveFormula = ReactiveCommand.CreateFromTask(async () =>
            {
                var loadedFile = LoadedFile;
                if (loadedFile != null)
                {
                    await File.WriteAllTextAsync(loadedFile, RatingFormula);
                    IsModified = false;
                }
                else
                {
                    await SaveFormulaWithSaveFileDialog.Execute();
                }
            });

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, EvaluationFormulaViewModel>(manager, nameof(manager.RecordChanged), OnRecordChanged);
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Metadata && args.AddedOrUpdated)
            {
                UpdateGroupKeys(evaluationManager.CurrentGrouping, args.File);
            }
        }

        private void UpdateGroupingConfiguration(GroupingConfiguration configuration)
        {
            // Setting a new grouping (configuration) also triggers the ratings
            // auto update, if enabled
            evaluationManager.CurrentGroupingConfiguration = configuration;

            UpdateGroupKeys(evaluationManager.CurrentGrouping);
        }

        private void UpdateGroupKeys(IGroupingManager.IGrouping? grouping, string? file = null)
        {
            using (DelayChangeNotifications())
            {
                if (file == null)
                {
                    GroupKeys.Clear();
                    GroupKeys.Add("All");

                    if (grouping != null)
                    {
                        foreach (var f in manager.Files)
                        {
                            var metadata = manager.Get(f)?.Metadata;
                            var match = metadata != null ? grouping.GetGroupMatch(metadata) : null;

                            if (match != null && !GroupKeys.Contains(match.GroupKey))
                            {
                                GroupKeys.Add(match.GroupKey);
                            }
                        }
                    }
                }
                else if (grouping != null)
                {
                    var metadata = manager.Get(file)?.Metadata;
                    var match = metadata != null ? grouping.GetGroupMatch(metadata) : null;

                    if (match != null && !GroupKeys.Contains(match.GroupKey))
                    {
                        GroupKeys.Add(match.GroupKey);
                    }
                }
            }
        }

        private async Task<Unit> UpdateEvaluatorInstanceAsync(string formula, CancellationToken cancel = default)
        {
            IEvaluationService.IEvaluator? evaluatorInstance = null;

            bool errored;

            if (formula != null && formula.Length > 0 && !evaluationService.Build(formula, out evaluatorInstance, out var errorMessage) && errorMessage != null)
            {
                errored = true;
                RatingFormulaError = errorMessage;
                try
                {
                    await RatingFormulaErrorDialog.Handle(errorMessage);
                }
                catch (UnhandledInteractionException<string, Unit>)
                {
                    // Don't care, the dialog is optional
                }
            }
            else
            {
                errored = false;
                RatingFormulaError = null;
            }

            EvaluatorInstance = errored ? null : evaluatorInstance;

            // Setting a new formula also triggers the ratings
            // auto update, if enabled
            evaluationManager.CurrentFormula = errored ? null : formula;

            return Unit.Default;
        }

        private async Task UpdateRatingsAsync()
        {
            await evaluationManager.UpdateRatingsAsync(null, EvaluatorInstance);
        }
    }
}
