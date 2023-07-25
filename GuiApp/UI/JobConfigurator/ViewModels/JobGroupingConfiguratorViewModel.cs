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

using Avalonia.Collections;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.UI.Evaluation;
using DryIocAttributes;
using System.ComponentModel.Composition;
using FitsRatingTool.IoC;
using FitsRatingTool.GuiApp.UI.KeywordPicker;
using DryIoc;

namespace FitsRatingTool.GuiApp.UI.JobConfigurator.ViewModels
{
    [Export(typeof(IJobGroupingConfiguratorViewModel)), TransientReuse]
    public class JobGroupingConfiguratorViewModel : ViewModelBase, IJobGroupingConfiguratorViewModel
    {
        public JobGroupingConfiguratorViewModel(IRegistrar<IJobGroupingConfiguratorViewModel, IJobGroupingConfiguratorViewModel.OfConfiguration> reg)
        {
            reg.RegisterAndReturn<JobGroupingConfiguratorViewModel>();
        }

        private class GroupingFitsKeywordViewModel : ReactiveObject, IJobGroupingConfiguratorViewModel.IGroupingFitsKeywordViewModel
        {
            public ReactiveCommand<Unit, Unit> Remove { get; }

            public IKeywordPickerViewModel KeywordPicker { get; }

            private string _keyword = "";
            public string Keyword
            {
                get => _keyword;
                set => this.RaiseAndSetIfChanged(ref _keyword, value);
            }

            public IDisposable? Disposable { get; set; }

            public GroupingFitsKeywordViewModel(IKeywordPickerViewModel keywordPicker)
            {
                KeywordPicker = keywordPicker;

                Remove = ReactiveCommand.Create(() => { });
            }
        }

        private bool _isGroupedByObject = false;
        public bool IsGroupedByObject
        {
            get => _isGroupedByObject;
            set => this.RaiseAndSetIfChanged(ref _isGroupedByObject, value);
        }

        private bool _isGroupedByFilter = false;
        public bool IsGroupedByFilter
        {
            get => _isGroupedByFilter;
            set => this.RaiseAndSetIfChanged(ref _isGroupedByFilter, value);
        }

        private bool _isGroupedByParentDir = false;
        public bool IsGroupedByParentDir
        {
            get => _isGroupedByParentDir;
            set => this.RaiseAndSetIfChanged(ref _isGroupedByParentDir, value);
        }

        private bool _isGroupedByFitsKeyword = false;
        public bool IsGroupedByFitsKeyword
        {
            get => _isGroupedByFitsKeyword;
            set => this.RaiseAndSetIfChanged(ref _isGroupedByFitsKeyword, value);
        }

        private bool _isGroupedByExposureTime = false;
        public bool IsGroupedByExposureTime
        {
            get => _isGroupedByExposureTime;
            set => this.RaiseAndSetIfChanged(ref _isGroupedByExposureTime, value);
        }

        private bool _isGroupedByGainAndOffset = false;
        public bool IsGroupedByGainAndOffset
        {
            get => _isGroupedByGainAndOffset;
            set => this.RaiseAndSetIfChanged(ref _isGroupedByGainAndOffset, value);
        }

        private int _groupingParentDirs = 1;
        public int GroupingParentDirs
        {
            get => _groupingParentDirs;
            set
            {
                this.RaiseAndSetIfChanged(ref _groupingParentDirs, Math.Max(value, 1));
                this.RaisePropertyChanged(nameof(IsSingleGroupingParentDir));
            }
        }

        public bool IsSingleGroupingParentDir
        {
            get => _groupingParentDirs <= 1;
        }

        public ReactiveCommand<Unit, Unit> IncreaseGroupingParentDirs { get; }

        public ReactiveCommand<Unit, Unit> DecreaseGroupingParentDirs { get; }

        public AvaloniaList<IJobGroupingConfiguratorViewModel.IGroupingFitsKeywordViewModel> GroupingFitsKeywords { get; } = new();

        public ReactiveCommand<Unit, Unit> AddNewGroupingFitsKeyword { get; }

        private GroupingConfiguration _groupingConfiguration = null!; // Is initialized in ctor
        public GroupingConfiguration GroupingConfiguration
        {
            get => _groupingConfiguration;
            set => this.RaiseAndSetIfChanged(ref _groupingConfiguration, value);
        }


        private readonly IContainer<IKeywordPickerViewModel, IKeywordPickerViewModel.OfCurrentlySelectedFile> keywordPickerContainer;
        private readonly GroupingConfiguration? sourceGroupingConfiguration;

        /*
         * Designer only ctor
         */
        public JobGroupingConfiguratorViewModel() : this(new IJobGroupingConfiguratorViewModel.OfConfiguration(), null!) { }

        private JobGroupingConfiguratorViewModel(IJobGroupingConfiguratorViewModel.OfConfiguration args,
            IContainer<IKeywordPickerViewModel, IKeywordPickerViewModel.OfCurrentlySelectedFile> keywordPickerContainer)
        {
            this.keywordPickerContainer = keywordPickerContainer;
            this.sourceGroupingConfiguration = args.Configuration;

            GroupingConfiguration = GetGroupingConfiguration();

            this.WhenAnyValue(x => x.IsGroupedByObject).Skip(1).Subscribe(x => UpdateGroupingConfiguration());
            this.WhenAnyValue(x => x.IsGroupedByFilter).Skip(1).Subscribe(x => UpdateGroupingConfiguration());
            this.WhenAnyValue(x => x.IsGroupedByExposureTime).Skip(1).Subscribe(x => UpdateGroupingConfiguration());
            this.WhenAnyValue(x => x.IsGroupedByGainAndOffset).Skip(1).Subscribe(x => UpdateGroupingConfiguration());
            this.WhenAnyValue(x => x.IsGroupedByParentDir).Skip(1).Subscribe(x => UpdateGroupingConfiguration());
            this.WhenAnyValue(x => x.GroupingParentDirs).Skip(1).Subscribe(x => UpdateGroupingConfiguration());
            this.WhenAnyValue(x => x.IsGroupedByFitsKeyword).Skip(1).Subscribe(x => UpdateGroupingConfiguration());

            GroupingFitsKeywords.CollectionChanged += (_, args) =>
            {
                // Only need to update when removed. Updating to add a new group
                // is done when the keyword text is changed.
                if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    UpdateGroupingConfiguration();
                }
            };

            IncreaseGroupingParentDirs = ReactiveCommand.Create(() =>
            {
                ++GroupingParentDirs;
            });

            DecreaseGroupingParentDirs = ReactiveCommand.Create(() =>
            {
                --GroupingParentDirs;
            });

            AddNewGroupingFitsKeyword = ReactiveCommand.Create(() =>
            {
                AddGroupingFitsKeyword();
            });
        }

        protected override void OnInstantiated()
        {
            var cfg = sourceGroupingConfiguration;

            if (cfg != null)
            {
                IsGroupedByObject = cfg.IsGroupedByObject;
                IsGroupedByFilter = cfg.IsGroupedByFilter;
                IsGroupedByExposureTime = cfg.IsGroupedByExposureTime;
                IsGroupedByGainAndOffset = cfg.IsGroupedByGainAndOffset;
                IsGroupedByParentDir = cfg.IsGroupedByParentDir;
                IsGroupedByFitsKeyword = cfg.IsGroupedByFitsKeyword;
                GroupingParentDirs = cfg.GroupingParentDirs;
                if (cfg.GroupingFitsKeywords != null)
                {
                    foreach (var keyword in cfg.GroupingFitsKeywords)
                    {
                        AddGroupingFitsKeyword(keyword);
                    }
                }
            }

            // Add one entry by default
            if (GroupingFitsKeywords.Count == 0)
            {
                AddGroupingFitsKeyword();
            }
        }

        public void AddGroupingFitsKeyword(string keyword = "")
        {
            var keywordPicker = keywordPickerContainer.Instantiate(new());

            var vm = new GroupingFitsKeywordViewModel(keywordPicker)
            {
                Keyword = keyword
            };

            var disposable = new CompositeDisposable();

            disposable.Add(vm.WhenAnyValue(x => x.Keyword).Skip(1).Subscribe(x => UpdateGroupingConfiguration()));

            disposable.Add(Disposable.Create(() =>
            {
                keywordPickerContainer.Destroy(keywordPicker);
            }));

            disposable.Add(vm.WhenAnyValue(x => x.Keyword).Skip(1).Subscribe(x =>
            {
                if (!keywordPicker.Select(x))
                {
                    keywordPicker.SelectedKeyword = null;
                }
            }));
            disposable.Add(vm.WhenAnyValue(x => x.KeywordPicker.SelectedKeyword).Skip(1).Where(x => x != null).Subscribe(x => vm.Keyword = x ?? ""));


            disposable.Add(vm.Remove.Subscribe(_ =>
            {
                disposable.Dispose();
                GroupingFitsKeywords.Remove(vm);
            }));

            vm.Disposable = disposable;

            GroupingFitsKeywords.Add(vm);
        }

        public void ClearGroupingFitsKeywords()
        {
            foreach (var vm in GroupingFitsKeywords)
            {
                if (vm is GroupingFitsKeywordViewModel g)
                {
                    g.Disposable?.Dispose();
                }
            }
            GroupingFitsKeywords.Clear();
        }

        private void UpdateGroupingConfiguration()
        {
            GroupingConfiguration = GetGroupingConfiguration();
        }

        public GroupingConfiguration GetGroupingConfiguration()
        {
            return new GroupingConfiguration(
                IsGroupedByObject, IsGroupedByFilter, IsGroupedByExposureTime,
                IsGroupedByGainAndOffset, IsGroupedByParentDir, IsGroupedByFitsKeyword, GroupingParentDirs,
                GroupingFitsKeywords?.Select(x => x.Keyword)?.OfType<string>()
                );
        }
    }
}
