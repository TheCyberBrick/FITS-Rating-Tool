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

using DryIocAttributes;
using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.Exporters.Services;
using FitsRatingTool.Exporters.Services.Impl;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.IoC;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.ComponentModel.Composition;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Exporters.ViewModels
{
    [Export(typeof(IComponentRegistration<IExporterConfiguratorViewModel>)), TransientReuse]
    public class FileMoverExporterConfiguratorRegistration : ComponentRegistrationOfContainer<IExporterConfiguratorViewModel, IFileMoverExporterConfiguratorViewModel, IFileMoverExporterConfiguratorViewModel.Of>
    {
        public FileMoverExporterConfiguratorRegistration() : base("file_mover", "File Mover", new IFileMoverExporterConfiguratorViewModel.Of()) { }
    }

    [Export(typeof(IFileMoverExporterConfiguratorViewModel)), TransientReuse]
    public class FileMoverExporterConfiguratorViewModel : RatingThresholdExporterConfiguratorViewModel<float>, IFileMoverExporterConfiguratorViewModel
    {
        public FileMoverExporterConfiguratorViewModel(IRegistrar<IFileMoverExporterConfiguratorViewModel, IFileMoverExporterConfiguratorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<FileMoverExporterConfiguratorViewModel>();
        }

        public override IBaseExporterConfiguratorViewModel.FileExtension? FileExtension => null;

        public override bool UsesPath => true;
        public override bool UsesExportValue => false;
        public override bool UsesExportGroupKey => false;
        public override bool UsesExportVariables => false;


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

        private FileMoverExporterConfiguratorViewModel(IFileMoverExporterConfiguratorViewModel.Of args, IFileMoverExporterFactory fileMoverExporterFactory, IFitsImageManager fitsImageManager)
        {
            this.fileMoverExporterFactory = fileMoverExporterFactory;
            this.fitsImageManager = fitsImageManager;

            SelectPathWithOpenFolderDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                Path = await SelectPathOpenFolderDialog.Handle(Unit.Default);
            });
        }

        protected override void Validate()
        {
            base.Validate();
            IsValid &= ParentDirs >= 0;
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
