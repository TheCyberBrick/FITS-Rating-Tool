﻿/*
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
using FitsRatingTool.Exporters.Services;
using FitsRatingTool.Exporters.Services.Impl;
using FitsRatingTool.GuiApp.Services;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Exporters.ViewModels
{
    public class FileMoverExporterConfiguratorViewModel : BaseExporterConfiguratorViewModel, IFileMoverExporterConfiguratorViewModel
    {
        public class Factory : IFileMoverExporterConfiguratorViewModel.IFactory
        {
            private IFileMoverExporterFactory fileMoverExporterFactory;
            private readonly IFitsImageManager fitsImageManager;

            public Factory(IFileMoverExporterFactory fileMoverExporterFactory, IFitsImageManager fitsImageManager)
            {
                this.fileMoverExporterFactory = fileMoverExporterFactory;
                this.fitsImageManager = fitsImageManager;
            }

            public IFileMoverExporterConfiguratorViewModel Create()
            {
                return new FileMoverExporterConfiguratorViewModel(fileMoverExporterFactory, fitsImageManager);
            }
        }

        public override IBaseExporterConfiguratorViewModel.FileExtension? FileExtension => null;

        public override bool UsesPath => true;
        public override bool UsesExportValue => false;
        public override bool UsesExportGroupKey => false;
        public override bool UsesExportVariables => false;


        private bool _isMinRatingThresholdEnabled;
        public bool IsMinRatingThresholdEnabled
        {
            get => _isMinRatingThresholdEnabled;
            set => this.RaiseAndSetIfChanged(ref _isMinRatingThresholdEnabled, value);
        }

        private float _minRatingThreshold;
        public float MinRatingThreshold
        {
            get => _minRatingThreshold;
            set => this.RaiseAndSetIfChanged(ref _minRatingThreshold, value);
        }

        private bool _isMaxRatingThresholdEnabled;
        public bool IsMaxRatingThresholdEnabled
        {
            get => _isMaxRatingThresholdEnabled;
            set => this.RaiseAndSetIfChanged(ref _isMaxRatingThresholdEnabled, value);
        }

        private float _maxRatingThreshold;
        public float MaxRatingThreshold
        {
            get => _maxRatingThreshold;
            set => this.RaiseAndSetIfChanged(ref _maxRatingThreshold, value);
        }

        private ObservableAsPropertyHelper<bool> _isLessThanRule;
        public bool IsLessThanRule => _isLessThanRule.Value;

        private ObservableAsPropertyHelper<bool> _isGreaterThanRule;
        public bool IsGreaterThanRule => _isGreaterThanRule.Value;

        private ObservableAsPropertyHelper<bool> _isLessThanOrGreaterThanRule;
        public bool IsLessThanOrGreaterThanRule => _isLessThanOrGreaterThanRule.Value;

        private bool _isRelativePath = true;
        public bool IsRelativePath
        {
            get => _isRelativePath;
            set => this.RaiseAndSetIfChanged(ref _isRelativePath, value);
        }

        private int _parentDirs = 0;
        public int ParentDirs
        {
            get => _parentDirs;
            set => this.RaiseAndSetIfChanged(ref _parentDirs, value);
        }

        public ReactiveCommand<Unit, Unit> SelectPathWithOpenFolderDialog { get; }

        public Interaction<Unit, string> SelectPathOpenFolderDialog { get; } = new();


        private readonly IFileMoverExporterFactory fileMoverExporterFactory;
        private readonly IFitsImageManager fitsImageManager;

        public FileMoverExporterConfiguratorViewModel(IFileMoverExporterFactory fileMoverExporterFactory, IFitsImageManager fitsImageManager)
        {
            this.fileMoverExporterFactory = fileMoverExporterFactory;
            this.fitsImageManager = fitsImageManager;

            var hasMin = this.WhenAnyValue(x => x.IsMinRatingThresholdEnabled);
            var hasMax = this.WhenAnyValue(x => x.IsMaxRatingThresholdEnabled);

            hasMin.Subscribe(x => NotifyConfigurationChange());
            hasMax.Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.MaxRatingThreshold).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.MinRatingThreshold).Subscribe(x => NotifyConfigurationChange());

            _isLessThanRule = Observable.CombineLatest(hasMin, hasMax, (a, b) => a && !b).ToProperty(this, x => x.IsLessThanRule);
            _isGreaterThanRule = Observable.CombineLatest(hasMin, hasMax, (a, b) => !a && b).ToProperty(this, x => x.IsGreaterThanRule);
            _isLessThanOrGreaterThanRule = Observable.CombineLatest(hasMin, hasMax, (a, b) => a && b).ToProperty(this, x => x.IsLessThanOrGreaterThanRule);

            SelectPathWithOpenFolderDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                Path = await SelectPathOpenFolderDialog.Handle(Unit.Default);
            });
        }

        protected override void Validate()
        {
            IsValid = Path.Length > 0 && (IsMinRatingThresholdEnabled || IsMaxRatingThresholdEnabled) && ParentDirs >= 0;
        }

        public override string CreateConfig()
        {
            var config = new FileMoverExporterFactory.Config
            {
                MinRatingThreshold = IsMinRatingThresholdEnabled ? MinRatingThreshold : null,
                MaxRatingThreshold = IsMaxRatingThresholdEnabled ? MaxRatingThreshold : null,
                Path = Path,
                IsRelativePath = IsRelativePath,
                ParentDirs = ParentDirs
            };
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        protected override bool DoTryLoadConfig(string config)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<FileMoverExporterFactory.Config>(config);

                if (cfg != null)
                {
                    IsMinRatingThresholdEnabled = cfg.MinRatingThreshold.HasValue;
                    MinRatingThreshold = cfg.MinRatingThreshold ?? 0;
                    IsMaxRatingThresholdEnabled = cfg.MaxRatingThreshold.HasValue;
                    MaxRatingThreshold = cfg.MaxRatingThreshold ?? 0;
                    Path = cfg.Path;
                    IsRelativePath = cfg.IsRelativePath;
                    ParentDirs = cfg.ParentDirs;

                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public override IEvaluationExporter CreateExporter(IEvaluationExporterContext ctx)
        {
            void onEvent(object? sender, IEvaluationExporterEventDispatcher.ExporterEventArgs e)
            {
                if (e.Name == IFileMoverExporterFactory.EVENT_MOVING_FILE && e.Parameter is IFileMoverExporterFactory.MovingFileEventParameters parameters)
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        fitsImageManager.Remove(parameters.OldFile);
                    });
                }
            }

            void onCleanup(object? sender, EventArgs e)
            {
                ctx.OnExporterEvent -= onEvent;
                ctx.OnExporterCleanup -= onCleanup;
            }

            ctx.OnExporterEvent += onEvent;
            ctx.OnExporterCleanup += onCleanup;

            return fileMoverExporterFactory.Create(ctx, CreateConfig());
        }
    }
}
