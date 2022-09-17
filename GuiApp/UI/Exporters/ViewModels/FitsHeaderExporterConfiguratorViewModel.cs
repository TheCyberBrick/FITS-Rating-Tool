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
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Linq;

namespace FitsRatingTool.GuiApp.UI.Exporters.ViewModels
{
    public class FitsHeaderExporterConfiguratorViewModel : BaseExporterConfiguratorViewModel, IFitsHeaderExporterConfiguratorViewModel
    {
        public class Factory : IFitsHeaderExporterConfiguratorViewModel.IFactory
        {
            private readonly IFitsHeaderEvaluationExporterFactory fitsHeaderEvaluationExporterFactory;

            public Factory(IFitsHeaderEvaluationExporterFactory fitsHeaderEvaluationExporterFactory)
            {
                this.fitsHeaderEvaluationExporterFactory = fitsHeaderEvaluationExporterFactory;
            }

            public IFitsHeaderExporterConfiguratorViewModel Create()
            {
                return new FitsHeaderExporterConfiguratorViewModel(fitsHeaderEvaluationExporterFactory);
            }
        }

        public override IBaseExporterConfiguratorViewModel.FileExtension? FileExtension => null;

        public override bool UsesPath => false;
        public override bool UsesExportValue => false;
        public override bool UsesExportGroupKey => false;
        public override bool UsesExportVariables => false;


        private string _keyword = "RATING";
        public string Keyword
        {
            get => _keyword;
            set => this.RaiseAndSetIfChanged(ref _keyword, value);
        }


        private readonly IFitsHeaderEvaluationExporterFactory fitsHeaderEvaluationExporterFactory;

        public FitsHeaderExporterConfiguratorViewModel(IFitsHeaderEvaluationExporterFactory fitsHeaderEvaluationExporterFactory) : base()
        {
            this.fitsHeaderEvaluationExporterFactory = fitsHeaderEvaluationExporterFactory;

            this.WhenAnyValue(x => x.Keyword).Subscribe(x => NotifyConfigurationChange());
        }

        protected override void Validate()
        {
            IsValid = Keyword.Length > 0 && Keyword.Length <= 8 && char.IsLetter(Keyword[0]) && Keyword.All(c => char.IsLetter(c) || char.IsDigit(c));
        }

        public override string CreateConfig()
        {
            var config = new FitsHeaderEvaluationExporterFactory.Config
            {
                Keyword = Keyword
            };
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        protected override bool DoTryLoadConfig(string config)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<FitsHeaderEvaluationExporterFactory.Config>(config);

                if (cfg != null)
                {
                    Keyword = cfg.Keyword;

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
            return fitsHeaderEvaluationExporterFactory.Create(ctx, CreateConfig());
        }
    }
}
