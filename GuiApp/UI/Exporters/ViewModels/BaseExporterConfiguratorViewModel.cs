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
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Collections;
using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.Common.Services.Impl;
using ReactiveUI;

namespace FitsRatingTool.GuiApp.UI.Exporters.ViewModels
{
    public abstract class BaseExporterConfiguratorViewModel : ViewModelBase, IBaseExporterConfiguratorViewModel
    {
        private class ExportVariablesFilterViewModel : ReactiveObject, IBaseExporterConfiguratorViewModel.IExportVariablesFilterViewModel
        {
            public ReactiveCommand<Unit, Unit> Remove { get; }

            private string? _variable;
            public string? Variable
            {
                get => _variable;
                set => this.RaiseAndSetIfChanged(ref _variable, value);
            }

            public IDisposable? Disposable { get; set; }

            public ExportVariablesFilterViewModel()
            {
                Remove = ReactiveCommand.Create(() => { });
            }
        }


        public abstract IBaseExporterConfiguratorViewModel.FileExtension? FileExtension { get; }


        public virtual bool UsesPath { get; } = true;

        private string _path = "";
        public string Path
        {
            get => _path;
            set => this.RaiseAndSetIfChanged(ref _path, value);
        }

        public ReactiveCommand<Unit, Unit> SelectPathWithSaveFileDialog { get; }

        public Interaction<IBaseExporterConfiguratorViewModel.FileExtension, string> SelectPathSaveFileDialog { get; } = new();


        public virtual bool UsesExportValue { get; } = true;

        private bool _exportValue = true;
        public bool ExportValue
        {
            get => _exportValue;
            set => this.RaiseAndSetIfChanged(ref _exportValue, value);
        }


        public virtual bool UsesExportGroupKey { get; } = true;

        private bool _exportGroupKey;
        public bool ExportGroupKey
        {
            get => _exportGroupKey;
            set => this.RaiseAndSetIfChanged(ref _exportGroupKey, value);
        }


        public virtual bool UsesExportVariables { get; } = true;

        private bool _exportVariables;
        public bool ExportVariables
        {
            get => _exportVariables;
            set => this.RaiseAndSetIfChanged(ref _exportVariables, value);
        }

        private bool _isExportVariablesFilterEnabled;
        public bool IsExportVariablesFilterEnabled
        {
            get => _isExportVariablesFilterEnabled;
            set => this.RaiseAndSetIfChanged(ref _isExportVariablesFilterEnabled, value);
        }

        public AvaloniaList<IBaseExporterConfiguratorViewModel.IExportVariablesFilterViewModel> ExportVariablesFilter { get; } = new();

        public ReactiveCommand<Unit, Unit> AddNewExportVariablesFilter { get; }



        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            protected set => this.RaiseAndSetIfChanged(ref _isValid, value);
        }


        public BaseExporterConfiguratorViewModel()
        {
            this.WhenAnyValue(x => x.Path).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.ExportValue).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.ExportGroupKey).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.ExportVariables).Skip(1).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.IsExportVariablesFilterEnabled).Skip(1).Subscribe(x => NotifyConfigurationChange());

            ExportVariablesFilter.CollectionChanged += (_, args) =>
            {
                // Only need to update when removed. Updating to add a new filter
                // is done when the variable text is changed.
                if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    NotifyConfigurationChange();
                }
            };

            // Add one entry by default
            if (ExportVariablesFilter.Count == 0)
            {
                AddExportVariablesFilter();
            }

            AddNewExportVariablesFilter = ReactiveCommand.Create(() =>
            {
                AddExportVariablesFilter();
            });

            SelectPathWithSaveFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                if (FileExtension != null)
                {
                    Path = await SelectPathSaveFileDialog.Handle(FileExtension);
                }
            });
        }

        public void AddExportVariablesFilter(string? variable = null)
        {
            var vm = new ExportVariablesFilterViewModel()
            {
                Variable = variable
            };

            var disposable = new CompositeDisposable();

            disposable.Add(vm.WhenAnyValue(x => x.Variable)
                .Skip(1)
                .Subscribe(x => NotifyConfigurationChange()));

            disposable.Add(vm.Remove.Subscribe(_ =>
            {
                disposable.Dispose();
                ExportVariablesFilter.Remove(vm);
            }));

            vm.Disposable = disposable;

            ExportVariablesFilter.Add(vm);
        }

        public void ClearExportVariablesFilters()
        {
            foreach (var vm in ExportVariablesFilter)
            {
                if (vm is ExportVariablesFilterViewModel e)
                {
                    e.Disposable?.Dispose();
                }
            }
            ExportVariablesFilter.Clear();
        }

        protected void NotifyConfigurationChange()
        {
            IsValid = true;
            Validate();
            _configurationChanged?.Invoke(this, new EventArgs());
        }

        protected virtual void Validate()
        {
            IsValid = !UsesPath || Path.Length > 0;
        }

        public abstract string CreateConfig();

        public abstract IEvaluationExporter CreateExporter(IEvaluationExporterContext ctx);


        public bool TryLoadConfig(string config)
        {
            bool loaded = DoTryLoadConfig(config);
            NotifyConfigurationChange();
            return loaded;
        }

        protected abstract bool DoTryLoadConfig(string config);

        private EventHandler? _configurationChanged;
        public event EventHandler ConfigurationChanged
        {
            add => _configurationChanged += value;
            remove => _configurationChanged -= value;
        }
    }
}
