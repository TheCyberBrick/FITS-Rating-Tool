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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.Common.Services;
using ReactiveUI;
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Evaluation;
using static FitsRatingTool.GuiApp.UI.JobConfigurator.IJobConfiguratorViewModel;
using System.Text.RegularExpressions;
using System.Reactive.Concurrency;
using System.Text;
using DryIocAttributes;
using System.ComponentModel.Composition;
using FitsRatingTool.GuiApp.UI.Exporters;
using FitsRatingTool.IoC;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using FitsRatingTool.GuiApp.UI.Variables;
using DynamicData;
using FitsRatingTool.GuiApp.UI.InstrumentProfile.ViewModels;
using FitsRatingTool.GuiApp.UI.Utils;

namespace FitsRatingTool.GuiApp.UI.JobConfigurator.ViewModels
{
    [Export(typeof(IJobConfiguratorViewModel)), TransientReuse]
    public class JobConfiguratorViewModel : ViewModelBase, IJobConfiguratorViewModel
    {
        public JobConfiguratorViewModel(IRegistrar<IJobConfiguratorViewModel, IJobConfiguratorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<JobConfiguratorViewModel>();
        }

        public JobConfiguratorViewModel(IRegistrar<IJobConfiguratorViewModel, IJobConfiguratorViewModel.OfConfigFile> reg)
        {
            reg.RegisterAndReturn<JobConfiguratorViewModel>();
        }


        private class VariableItemViewModel : ViewModelBase, IVariableItemViewModel
        {
            public IVariableEditorViewModel Editor { get; }

            public ReactiveCommand<Unit, Unit> Remove { get; }


            public VariableItemViewModel(IVariableEditorViewModel editor)
            {
                Editor = editor;
                Remove = ReactiveCommand.Create(() => { });
            }
        }


        private class GroupingFilterViewModel : ReactiveObject, IGroupingFilterViewModel
        {
            public ReactiveCommand<Unit, Unit> Remove { get; }

            private string? _key;
            public string? Key
            {
                get => _key;
                set => this.RaiseAndSetIfChanged(ref _key, value);
            }

            private bool _isKeyValid;
            public bool IsKeyValid
            {
                get => _isKeyValid;
                set => this.RaiseAndSetIfChanged(ref _isKeyValid, value);
            }

            private string? _pattern;
            public string? Pattern
            {
                get => _pattern;
                set => this.RaiseAndSetIfChanged(ref _pattern, value);
            }

            private bool _isPatternValid;
            public bool IsPatternValid
            {
                get => _isPatternValid;
                set => this.RaiseAndSetIfChanged(ref _isPatternValid, value);
            }

            public IDisposable? Disposable { get; set; }

            public GroupingFilterViewModel(IGroupingManager groupingManager)
            {
                Remove = ReactiveCommand.Create(() => { });

                this.WhenAnyValue(x => x.Key).Subscribe(x =>
                {
                    IsKeyValid = x != null && x.Length > 0 ? groupingManager.TryParseGroupingKey(x, out var _, out var _) : true;
                });

                this.WhenAnyValue(x => x.Pattern).Subscribe(x =>
                {
                    if (x == null || x.Length == 0)
                    {
                        IsPatternValid = true;
                    }
                    else
                    {
                        try
                        {
                            Regex.Match("", x);
                            IsPatternValid = true;
                        }
                        catch (Exception)
                        {
                            IsPatternValid = false;
                        }
                    }
                });
            }
        }


        public AvaloniaList<IVariableItemViewModel> Variables { get; } = new();

        public ReactiveCommand<Unit, Unit> AddVariable { get; }

        private bool _isVariableValid = true;
        private bool IsVariableValid
        {
            get => _isVariableValid;
            set => this.RaiseAndSetIfChanged(ref _isVariableValid, value);
        }


        private IEvaluationFormulaViewModel _evaluationFormula = null!;
        public IEvaluationFormulaViewModel EvaluationFormula
        {
            get => _evaluationFormula;
            private set => this.RaiseAndSetIfChanged(ref _evaluationFormula, value);
        }


        private IJobGroupingConfiguratorViewModel _groupingConfigurator = null!;
        public IJobGroupingConfiguratorViewModel GroupingConfigurator
        {
            get => _groupingConfigurator;
            private set => this.RaiseAndSetIfChanged(ref _groupingConfigurator, value);
        }

        private bool _groupingKeysRequired;
        public bool GroupingKeysRequired
        {
            get => _groupingKeysRequired;
            set => this.RaiseAndSetIfChanged(ref _groupingKeysRequired, value);
        }

        private bool _isFilteredByGrouping;
        public bool IsFilteredByGrouping
        {
            get => _isFilteredByGrouping;
            set => this.RaiseAndSetIfChanged(ref _isFilteredByGrouping, value);
        }

        public AvaloniaList<IGroupingFilterViewModel> GroupingFilters { get; } = new();

        public ReactiveCommand<Unit, Unit> AddNewGroupingFilter { get; }

        private bool _isGroupingFilterValid = true;
        private bool IsGroupingFilterValid
        {
            get => _isGroupingFilterValid;
            set => this.RaiseAndSetIfChanged(ref _isGroupingFilterValid, value);
        }


        private int _parallelIO = 1;
        public int ParallelIO
        {
            get => _parallelIO;
            set => this.RaiseAndSetIfChanged(ref _parallelIO, value);
        }

        private int _parallelTasks = 4;
        public int ParallelTasks
        {
            get => _parallelTasks;
            set => this.RaiseAndSetIfChanged(ref _parallelTasks, value);
        }



        private long _maxImageSize = 805306368;
        public long MaxImageSize
        {
            get => _maxImageSize;
            set => this.RaiseAndSetIfChanged(ref _maxImageSize, value);
        }

        private int _maxImageWidth = 8192;
        public int MaxImageWidth
        {
            get => _maxImageWidth;
            set => this.RaiseAndSetIfChanged(ref _maxImageWidth, value);
        }

        private int _maxImageHeight = 8192;
        public int MaxImageHeight
        {
            get => _maxImageHeight;
            set => this.RaiseAndSetIfChanged(ref _maxImageHeight, value);
        }



        private string _outputLogsPath = "";
        public string OutputLogsPath
        {
            get => _outputLogsPath;
            set => this.RaiseAndSetIfChanged(ref _outputLogsPath, value);
        }

        public ReactiveCommand<Unit, Unit> SelectOutputLogsPathWithOpenFolderDialog { get; }

        public Interaction<Unit, string> SelectOutputLogsPathOpenFolderDialog { get; } = new();



        private string _cachePath = "";
        public string CachePath
        {
            get => _cachePath;
            set => this.RaiseAndSetIfChanged(ref _cachePath, value);
        }

        public ReactiveCommand<Unit, Unit> SelectCachePathWithOpenFolderDialog { get; }

        public Interaction<Unit, string> SelectCachePathOpenFolderDialog { get; } = new();



        private IEvaluationExporterConfiguratorViewModel _evaluationExporterConfigurator = null!;
        public IEvaluationExporterConfiguratorViewModel EvaluationExporterConfigurator
        {
            get => _evaluationExporterConfigurator;
            private set => this.RaiseAndSetIfChanged(ref _evaluationExporterConfigurator, value);
        }



        private IReadOnlyJobConfig _jobConfig = null!; // Is set in ctor
        public IReadOnlyJobConfig JobConfig
        {
            get => _jobConfig;
            private set => this.RaiseAndSetIfChanged(ref _jobConfig, value);
        }



        public ReactiveCommand<Unit, Unit> SaveJobConfigWithSaveFileDialog { get; }

        public Interaction<Unit, string> SaveJobConfigSaveFileDialog { get; } = new();

        public Interaction<SaveResult, Unit> SaveJobConfigResultDialog { get; } = new();


        public Interaction<Exception, Unit> ConfigLoadErrorDialog { get; } = new();


        private readonly IJobConfigFactory jobConfigFactory;
        private readonly IGroupingManager groupingManager;

        private readonly IContainer<IVariableEditorViewModel, IVariableEditorViewModel.Of> variableEditorContainer;

        private readonly string? configFile;

        private JobConfiguratorViewModel(IJobConfiguratorViewModel.OfConfigFile args,
            IEvaluationManager evaluationManager,
            IJobConfigFactory jobConfigFactory,
            IGroupingManager groupingManager,
            IContainer<IJobGroupingConfiguratorViewModel, IJobGroupingConfiguratorViewModel.OfConfiguration> groupingConfiguratorContainer,
            IContainer<IEvaluationFormulaViewModel, IEvaluationFormulaViewModel.Of> evaluationFormulaContainer,
            IContainer<IEvaluationExporterConfiguratorViewModel, IEvaluationExporterConfiguratorViewModel.Of> evaluationExporterConfiguratorContainer,
            IContainer<IVariableEditorViewModel, IVariableEditorViewModel.Of> variableEditorContainer)
            : this(args.File, evaluationManager, jobConfigFactory, groupingManager, groupingConfiguratorContainer, evaluationFormulaContainer, evaluationExporterConfiguratorContainer, variableEditorContainer)
        {
        }

        private JobConfiguratorViewModel(IJobConfiguratorViewModel.Of args,
            IEvaluationManager evaluationManager,
            IJobConfigFactory jobConfigFactory,
            IGroupingManager groupingManager,
            IContainer<IJobGroupingConfiguratorViewModel, IJobGroupingConfiguratorViewModel.OfConfiguration> groupingConfiguratorContainer,
            IContainer<IEvaluationFormulaViewModel, IEvaluationFormulaViewModel.Of> evaluationFormulaContainer,
            IContainer<IEvaluationExporterConfiguratorViewModel, IEvaluationExporterConfiguratorViewModel.Of> evaluationExporterConfiguratorContainer,
            IContainer<IVariableEditorViewModel, IVariableEditorViewModel.Of> variableEditorContainer)
            : this((string?)null, evaluationManager, jobConfigFactory, groupingManager, groupingConfiguratorContainer, evaluationFormulaContainer, evaluationExporterConfiguratorContainer, variableEditorContainer)
        {
        }

        private JobConfiguratorViewModel(
            string? configFile,
            IEvaluationManager evaluationManager,
            IJobConfigFactory jobConfigFactory,
            IGroupingManager groupingManager,
            IContainer<IJobGroupingConfiguratorViewModel, IJobGroupingConfiguratorViewModel.OfConfiguration> groupingConfiguratorContainer,
            IContainer<IEvaluationFormulaViewModel, IEvaluationFormulaViewModel.Of> evaluationFormulaContainer,
            IContainer<IEvaluationExporterConfiguratorViewModel, IEvaluationExporterConfiguratorViewModel.Of> evaluationExporterConfiguratorContainer,
            IContainer<IVariableEditorViewModel, IVariableEditorViewModel.Of> variableEditorContainer)
        {
            this.configFile = configFile;
            this.jobConfigFactory = jobConfigFactory;
            this.groupingManager = groupingManager;

            this.variableEditorContainer = variableEditorContainer;

            evaluationExporterConfiguratorContainer.Singleton().Inject(new IEvaluationExporterConfiguratorViewModel.Of(), vm =>
            {
                EvaluationExporterConfigurator = vm;
                EvaluationExporterConfigurator.ConfigurationChanged += (s, e) => UpdateJobConfig();
            });

            SelectOutputLogsPathWithOpenFolderDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                OutputLogsPath = await SelectOutputLogsPathOpenFolderDialog.Handle(Unit.Default);
            });
            SelectCachePathWithOpenFolderDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                CachePath = await SelectCachePathOpenFolderDialog.Handle(Unit.Default);
            });

            var defaultGroupingConfiguration = evaluationManager.CurrentGroupingConfiguration;
            if (defaultGroupingConfiguration == null)
            {
                // By default group by object and filter
                defaultGroupingConfiguration = new GroupingConfiguration(true, true, false, false, false, false, 0, null);
            }

            groupingConfiguratorContainer.Singleton().Inject(new IJobGroupingConfiguratorViewModel.OfConfiguration(defaultGroupingConfiguration), vm => GroupingConfigurator = vm);

            evaluationFormulaContainer.Singleton().Inject(new IEvaluationFormulaViewModel.Of(), vm =>
            {
                EvaluationFormula = vm;
                EvaluationFormula.RatingFormula = evaluationManager.CurrentFormula;
            });

            GroupingFilters.CollectionChanged += (_, args) =>
            {
                // Only need to update when removed. Updating to add a new filter
                // is done when the key or pattern text is changed.
                if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    UpdateJobConfig();
                }
            };

            var canSave = Observable.CombineLatest(
                this.WhenAnyValue(x => x.EvaluationExporterConfigurator.ExporterConfigurator),
                this.WhenAnyValue(x => x.EvaluationExporterConfigurator.ExporterConfigurator!.IsValid).Prepend(true).DefaultIfEmpty(true),
                this.WhenAnyValue(x => x.EvaluationFormula.RatingFormulaError).Select(x => x == null),
                this.WhenAnyValue(x => x.IsFilteredByGrouping),
                this.WhenAnyValue(x => x.IsGroupingFilterValid),
                this.WhenAnyValue(x => x.IsVariableValid),
                (cfg, cfgValid, formulaValid, filteredByGrouping, groupingFilterValid, variableValid) => (cfg == null || cfgValid) && formulaValid && (!filteredByGrouping || groupingFilterValid) && variableValid);

            SaveJobConfigWithSaveFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var file = await SaveJobConfigSaveFileDialog.Handle(Unit.Default);
                string config = "";
                Exception? exception = null;
                try
                {
                    config = await SaveConfigAsync(file);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
                try
                {
                    await SaveJobConfigResultDialog.Handle(new SaveResult(file, config, exception));
                }
                catch (UnhandledInteractionException<SaveResult, Unit>)
                {
                    // OK, dialog is optional
                }
            }, canSave);

            AddVariable = ReactiveCommand.Create(() =>
            {
                CreateVariable();
            });

            AddNewGroupingFilter = ReactiveCommand.Create(() =>
            {
                AddGroupingFilter();
            });

            Variables.CollectionChanged += (sender, e) =>
            {
                ValidateVariables();
            };
        }

        protected override void OnInstantiated()
        {
            this.WhenAnyValue(x => x.GroupingConfigurator.GroupingConfiguration).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.EvaluationFormula.RatingFormula).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.GroupingKeysRequired).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.IsFilteredByGrouping).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.ParallelIO).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.ParallelTasks).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.MaxImageSize).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.MaxImageWidth).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.MaxImageHeight).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.OutputLogsPath).Subscribe(x => UpdateJobConfig());
            this.WhenAnyValue(x => x.CachePath).Subscribe(x => UpdateJobConfig());

            ValidateVariables();

            UpdateJobConfig();

            // Add one entry by default
            if (GroupingFilters.Count == 0)
            {
                AddGroupingFilter();
            }

            if (configFile != null)
            {
                RxApp.MainThreadScheduler.Schedule(() => TryImportConfigFile(configFile));
            }
        }

        private IVariableItemViewModel CreateVariable()
        {
            var item = new VariableItemViewModel(variableEditorContainer.Instantiate(new()));

            item.Remove.Subscribe(_ =>
            {
                RemoveVariable(item);
            });

            SubscribeToEvent<IItemEditorViewModel<IVariableConfiguratorViewModel>, EventArgs, JobConfiguratorViewModel>(item.Editor, nameof(item.Editor.ConfigurationChanged), OnVariableConfiguratorChanged);

            Variables.Add(item);

            return item;
        }

        private void RemoveVariable(IVariableItemViewModel item)
        {
            Variables.Remove(item);

            variableEditorContainer.Destroy(item.Editor);

            UnsubscribeFromEvent<IItemEditorViewModel<IVariableConfiguratorViewModel>, EventArgs, JobConfiguratorViewModel>(item.Editor, nameof(item.Editor.ConfigurationChanged), OnVariableConfiguratorChanged);
        }

        private void OnVariableConfiguratorChanged(object? sender, EventArgs args)
        {
            ValidateVariables();
        }

        private void ValidateVariables()
        {
            bool valid = true;
            foreach (var variable in Variables)
            {
                if (!variable.Editor.IsValid)
                {
                    valid = false;
                    break;
                }
            }
            IsVariableValid = valid;

            UpdateJobConfig();
        }

        private async void TryImportConfigFile(string file)
        {
            Exception? error = null;

            try
            {
                var config = jobConfigFactory.Load(await File.ReadAllTextAsync(file, Encoding.UTF8));

                if (!TryLoadJobConfig(config))
                {
                    error = new Exception("Failed loading job config into job configurator");
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                try
                {
                    await ConfigLoadErrorDialog.Handle(error);
                }
                catch (UnhandledInteractionException<Exception, Unit>)
                {
                    // OK
                }
            }
        }

        public void AddGroupingFilter(string? key = null, string? pattern = null)
        {
            var vm = new GroupingFilterViewModel(groupingManager)
            {
                Key = key,
                Pattern = pattern
            };

            var disposable = new CompositeDisposable();

            disposable.Add(vm.WhenAnyValue(x => x.Key)
                .Skip(1)
                .Subscribe(x => UpdateJobConfig()));

            disposable.Add(vm.WhenAnyValue(x => x.Pattern)
                .Skip(1)
                .Subscribe(x => UpdateJobConfig()));

            disposable.Add(vm.WhenAnyValue(x => x.IsKeyValid)
                .Skip(1)
                .Subscribe(x => ValidateGroupingFilters()));

            disposable.Add(vm.WhenAnyValue(x => x.IsPatternValid)
                .Skip(1)
                .Subscribe(x => ValidateGroupingFilters()));

            disposable.Add(vm.Remove.Subscribe(_ =>
            {
                disposable.Dispose();
                GroupingFilters.Remove(vm);
            }));

            vm.Disposable = disposable;

            GroupingFilters.Add(vm);
        }

        public void ClearGroupingFilters()
        {
            foreach (var vm in GroupingFilters)
            {
                if (vm is GroupingFilterViewModel g)
                {
                    g.Disposable?.Dispose();
                }
            }
            GroupingFilters.Clear();
        }

        private void ValidateGroupingFilters()
        {
            bool valid = true;
            foreach (var vm in GroupingFilters)
            {
                if (!vm.IsKeyValid || !vm.IsPatternValid)
                {
                    valid = false;
                    break;
                }
            }
            IsGroupingFilterValid = valid;
        }

        private void UpdateJobConfig()
        {
            var config = jobConfigFactory.Builder().Build();

            config.EvaluationFormula = EvaluationFormula.RatingFormula ?? "";

            config.GroupingKeys = GroupingConfigurator.GroupingConfiguration.GroupingKeys;
            config.GroupingKeysRequired = GroupingKeysRequired;

            if (IsFilteredByGrouping)
            {
                var groupingFilters = GroupingFilters.Select(x => x.Key != null && x.Pattern != null ? new IReadOnlyJobConfig.FilterConfig(x.Key, x.Pattern) : null).OfType<IReadOnlyJobConfig.FilterConfig>();
                if (groupingFilters.Any())
                {
                    config.GroupingFilters = groupingFilters.ToArray();
                }
            }

            config.MaxImageSize = MaxImageSize;
            config.MaxImageWidth = MaxImageWidth;
            config.MaxImageHeight = MaxImageHeight;

            config.ParallelIO = ParallelIO;
            config.ParallelTasks = ParallelTasks;

            if (OutputLogsPath.Length > 0) config.OutputLogsPath = OutputLogsPath;
            if (CachePath.Length > 0) config.CachePath = CachePath;

            var exporters = new List<IReadOnlyJobConfig.ExporterConfig>();
            if (EvaluationExporterConfigurator.SelectedExporterConfiguratorFactory != null && EvaluationExporterConfigurator.ExporterConfigurator != null && EvaluationExporterConfigurator.ExporterConfigurator.IsValid)
            {
                exporters.Add(new IReadOnlyJobConfig.ExporterConfig(EvaluationExporterConfigurator.SelectedExporterConfiguratorFactory.Id, EvaluationExporterConfigurator.ExporterConfigurator.CreateConfig()));
            }
            config.Exporters = exporters;

            if (Variables != null)
            {
                var variables = new List<IReadOnlyJobConfig.VariableConfig>();

                foreach (var item in Variables)
                {
                    var configurator = item.Editor.Configurator;
                    var selectorItem = item.Editor.Selector.SelectedItem;
                    if (configurator != null && selectorItem != null)
                    {
                        var variable = configurator.CreateVariable();
                        var varCfg = configurator.CreateConfig();
                        variables.Add(new IReadOnlyJobConfig.VariableConfig(selectorItem.Id, variable.Name, varCfg));
                    }
                }

                config.Variables = variables;
            }

            JobConfig = config;
        }

        private async Task<string> SaveConfigAsync(string file)
        {
            var config = jobConfigFactory.Save(JobConfig);
            await File.WriteAllTextAsync(file, config);
            return config;
        }

        public bool TryLoadJobConfig(IReadOnlyJobConfig config)
        {
            if (GroupingConfiguration.TryParseGroupingKeys(groupingManager, config.GroupingKeys ?? new List<string>(), out var groupingConfiguration) && groupingConfiguration != null)
            {
                IEvaluationExporterConfiguratorViewModel.ExporterConfiguratorFactory? exporterFactory = null;
                IDelegatedFactory<IExporterConfiguratorViewModel>? exporterDelegatedFactory = null;

                if (config.Exporters != null)
                {
                    foreach (var exporter in config.Exporters)
                    {
                        if (exporterDelegatedFactory != null)
                        {
                            // Multiple exporters can't be handled in GUI
                            return false;
                        }

                        var factory = EvaluationExporterConfigurator.ExporterConfiguratorFactories.Where(f => f.Id.Equals(exporter.Id)).FirstOrDefault();
                        if (factory != null)
                        {
                            exporterFactory = factory;
                            exporterDelegatedFactory = factory.Factory;

                            if (!exporterDelegatedFactory.Do(exporterConfigurator => exporterConfigurator.TryLoadConfig(exporter.Config)))
                            {
                                // Invalid config
                                return false;
                            }

                            // Make exporter configurator load config on instantiation
                            exporterDelegatedFactory = exporterDelegatedFactory.AndThen(exporterConfigurator => exporterConfigurator.TryLoadConfig(exporter.Config));
                        }
                        else
                        {
                            // Unknown exporter
                            return false;
                        }
                    }
                }

                EvaluationFormula.RatingFormula = config.EvaluationFormula;

                GroupingConfigurator.ClearGroupingFitsKeywords();

                GroupingConfigurator.IsGroupedByObject = groupingConfiguration.IsGroupedByObject;
                GroupingConfigurator.IsGroupedByFilter = groupingConfiguration.IsGroupedByFilter;
                GroupingConfigurator.IsGroupedByExposureTime = groupingConfiguration.IsGroupedByExposureTime;
                GroupingConfigurator.IsGroupedByGainAndOffset = groupingConfiguration.IsGroupedByGainAndOffset;
                GroupingConfigurator.IsGroupedByParentDir = groupingConfiguration.IsGroupedByParentDir;
                GroupingConfigurator.IsGroupedByFitsKeyword = groupingConfiguration.IsGroupedByFitsKeyword;
                GroupingConfigurator.GroupingParentDirs = groupingConfiguration.GroupingParentDirs;

                if (groupingConfiguration.GroupingFitsKeywords != null)
                {
                    foreach (var keyword in groupingConfiguration.GroupingFitsKeywords)
                    {
                        GroupingConfigurator.AddGroupingFitsKeyword(keyword);
                    }
                }

                GroupingKeysRequired = config.GroupingKeysRequired;

                ClearGroupingFilters();

                IsFilteredByGrouping = config.GroupingFilters != null;
                if (config.GroupingFilters != null)
                {
                    foreach (var filter in config.GroupingFilters)
                    {
                        AddGroupingFilter(filter.Key, filter.Pattern);
                    }
                }

                ParallelIO = config.ParallelIO;
                ParallelTasks = config.ParallelTasks;

                MaxImageSize = config.MaxImageSize;
                MaxImageWidth = config.MaxImageWidth;
                MaxImageHeight = config.MaxImageHeight;

                OutputLogsPath = config.OutputLogsPath ?? "";
                CachePath = config.CachePath ?? "";

                EvaluationExporterConfigurator.SetExporterConfigurator(exporterDelegatedFactory);

                // Remove all variables first
                while (Variables.Count > 0)
                {
                    RemoveVariable(Variables[Variables.Count - 1]);
                }
                // And then try loading them from configs
                if (config.Variables != null)
                {
                    foreach (var varCfg in config.Variables)
                    {
                        var item = CreateVariable();

                        bool loaded = false;

                        try
                        {
                            loaded = item.Editor.Configure(varCfg.Id, configurator =>
                            {
                                return configurator.TryLoadConfig(varCfg.Name, varCfg.Config);
                            });
                        }
                        finally
                        {
                            if (!loaded)
                            {
                                RemoveVariable(item);
                            }
                        }

                        if (!loaded)
                        {
                            return false;
                        }
                    }
                }

                ValidateVariables();


                UpdateJobConfig();

                return true;
            }

            return false;
        }
    }
}
