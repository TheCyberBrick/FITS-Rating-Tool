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
using System.IO;
using System.Reactive;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Exporters.ViewModels
{
    public class VoyagerExporterConfiguratorViewModel : RatingThresholdExporterConfiguratorViewModel<int>, IVoyagerExporterConfiguratorViewModel
    {
        public class Factory : IVoyagerExporterConfiguratorViewModel.IFactory
        {
            private IVoyagerEvaluationExporterFactory voyagerEvaluationExporterFactory;
            private IAppConfig appConfig;

            public Factory(IVoyagerEvaluationExporterFactory voyagerEvaluationExporterFactory, IAppConfig appConfig)
            {
                this.voyagerEvaluationExporterFactory = voyagerEvaluationExporterFactory;
                this.appConfig = appConfig;
            }

            public IVoyagerExporterConfiguratorViewModel Create()
            {
                return new VoyagerExporterConfiguratorViewModel(voyagerEvaluationExporterFactory, appConfig);
            }
        }

        public override IBaseExporterConfiguratorViewModel.FileExtension? FileExtension => null;

        public override bool UsesPath => false;
        public override bool UsesExportValue => false;
        public override bool UsesExportGroupKey => false;
        public override bool UsesExportVariables => false;



        private string _applicationServerHostname = "localhost";
        public string ApplicationServerHostname
        {
            get => _applicationServerHostname;
            set => this.RaiseAndSetIfChanged(ref _applicationServerHostname, value);
        }

        private int _applicationServerPort = 5950;
        public int ApplicationServerPort
        {
            get => _applicationServerPort;
            set => this.RaiseAndSetIfChanged(ref _applicationServerPort, value);
        }

        private string _credentialsFile = "%userprofile%/voyager_credentials.json";
        public string CredentialsFile
        {
            get => _credentialsFile;
            set => this.RaiseAndSetIfChanged(ref _credentialsFile, value);
        }

        public ReactiveCommand<Unit, Unit> SelectCredentialsFileWithOpenFileDialog { get; }

        public ReactiveCommand<Unit, Unit> CreateCredentialsFileWithSaveFileDialog { get; }

        public Interaction<string, string> SelectCredentialsFileOpenFileDialog { get; } = new();

        public Interaction<string, string> CreateCredentialsFileSaveFileDialog { get; } = new();


        private readonly IVoyagerEvaluationExporterFactory voyagerEvaluationExporterFactory;

        public VoyagerExporterConfiguratorViewModel(IVoyagerEvaluationExporterFactory voyagerEvaluationExporterFactory, IAppConfig appConfig)
        {
            this.voyagerEvaluationExporterFactory = voyagerEvaluationExporterFactory;

            this.WhenAnyValue(x => x.ApplicationServerHostname).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.ApplicationServerPort).Subscribe(x => NotifyConfigurationChange());
            this.WhenAnyValue(x => x.CredentialsFile).Subscribe(x => NotifyConfigurationChange());

            SelectCredentialsFileWithOpenFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                CredentialsFile = await SelectCredentialsFileOpenFileDialog.Handle(CredentialsFile);
            });

            CreateCredentialsFileWithSaveFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                CredentialsFile = await CreateCredentialsFileSaveFileDialog.Handle(CredentialsFile);

                if (CredentialsFile.Length > 0)
                {
                    IVoyagerEvaluationExporterFactory.IVoyagerCredentials credentials = voyagerEvaluationExporterFactory.CreateCredentials();
                    credentials.ApplicationServerUsername = appConfig.VoyagerUsername;
                    credentials.ApplicationServerPassword = appConfig.VoyagerPassword;
                    credentials.RoboTargetSecret = appConfig.RoboTargetSecret;
                    File.WriteAllText(CredentialsFile, voyagerEvaluationExporterFactory.SaveCredentials(credentials));
                }
            });
        }

        protected override void Validate()
        {
            IsValid = ApplicationServerHostname.Length > 0 && CredentialsFile.Length > 0;
        }

        public override string CreateConfig()
        {
            var config = new VoyagerEvaluationExporterFactory.Config
            {
                ApplicationServerHostname = ApplicationServerHostname,
                ApplicationServerPort = ApplicationServerPort,
                CredentialsFile = CredentialsFile,
                MinRatingThreshold = IsMinRatingThresholdEnabled ? MinRatingThreshold : null,
                MaxRatingThreshold = IsMaxRatingThresholdEnabled ? MaxRatingThreshold : null
            };
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        protected override bool DoTryLoadConfig(string config)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<VoyagerEvaluationExporterFactory.Config>(config);

                if (cfg != null)
                {
                    ApplicationServerHostname = cfg.ApplicationServerHostname;
                    ApplicationServerPort = cfg.ApplicationServerPort;
                    CredentialsFile = cfg.CredentialsFile;
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
            return voyagerEvaluationExporterFactory.Create(ctx, CreateConfig());
        }
    }
}
