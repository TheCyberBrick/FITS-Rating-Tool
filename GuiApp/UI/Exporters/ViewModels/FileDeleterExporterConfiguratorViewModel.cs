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
using FitsRatingTool.Exporters.Services;
using FitsRatingTool.Exporters.Services.Impl;
using FitsRatingTool.GuiApp.Services;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Exporters.ViewModels
{
    public class FileDeleterExporterConfiguratorViewModel : RatingThresholdExporterConfiguratorViewModel<float>, IFileDeleterExporterConfiguratorViewModel
    {
        public class Factory : IFileDeleterExporterConfiguratorViewModel.IFactory
        {
            private IFileDeleterExporterFactory fileDeleterExporterFactory;
            private readonly IFitsImageManager fitsImageManager;

            public Factory(IFileDeleterExporterFactory fileDeleterExporterFactory, IFitsImageManager fitsImageManager)
            {
                this.fileDeleterExporterFactory = fileDeleterExporterFactory;
                this.fitsImageManager = fitsImageManager;
            }

            public IFileDeleterExporterConfiguratorViewModel Create()
            {
                return new FileDeleterExporterConfiguratorViewModel(fileDeleterExporterFactory, fitsImageManager);
            }
        }

        public override IBaseExporterConfiguratorViewModel.FileExtension? FileExtension => null;

        public override bool UsesPath => false;
        public override bool UsesExportValue => false;
        public override bool UsesExportGroupKey => false;
        public override bool UsesExportVariables => false;


        private readonly IFileDeleterExporterFactory fileDeleterExporterFactory;
        private readonly IFitsImageManager fitsImageManager;

        public FileDeleterExporterConfiguratorViewModel(IFileDeleterExporterFactory fileDeleterExporterFactory, IFitsImageManager fitsImageManager)
        {
            this.fileDeleterExporterFactory = fileDeleterExporterFactory;
            this.fitsImageManager = fitsImageManager;
        }

        public override string CreateConfig()
        {
            var config = new FileDeleterExporterFactory.Config
            {
                MinRatingThreshold = IsMinRatingThresholdEnabled ? MinRatingThreshold : null,
                MaxRatingThreshold = IsMaxRatingThresholdEnabled ? MaxRatingThreshold : null
            };
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        protected override bool DoTryLoadConfig(string config)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<FileDeleterExporterFactory.Config>(config);

                if (cfg != null)
                {
                    IsMinRatingThresholdEnabled = cfg.MinRatingThreshold.HasValue;
                    MinRatingThreshold = cfg.MinRatingThreshold ?? 0;
                    IsMaxRatingThresholdEnabled = cfg.MaxRatingThreshold.HasValue;
                    MaxRatingThreshold = cfg.MaxRatingThreshold ?? 0;

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
                if (e.Parameter is IFileDeleterExporterFactory.DeletingFileEventParameters parameters)
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        fitsImageManager.Remove(parameters.File);
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

            return fileDeleterExporterFactory.Create(ctx, CreateConfig());
        }
    }
}
