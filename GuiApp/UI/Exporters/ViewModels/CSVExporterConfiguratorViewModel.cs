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
using System;
using System.Linq;

namespace FitsRatingTool.GuiApp.UI.Exporters.ViewModels
{
    public class CSVExporterConfiguratorViewModel : BaseExporterConfiguratorViewModel, ICSVExporterConfiguratorViewModel
    {
        public class Factory : ICSVExporterConfiguratorViewModel.IFactory
        {
            private readonly ICSVEvaluationExporterFactory csvEvaluationExporterFactory;

            public Factory(ICSVEvaluationExporterFactory csvEvaluationExporterFactory)
            {
                this.csvEvaluationExporterFactory = csvEvaluationExporterFactory;
            }

            public ICSVExporterConfiguratorViewModel Create()
            {
                return new CSVExporterConfiguratorViewModel(csvEvaluationExporterFactory);
            }
        }

        public override IBaseExporterConfiguratorViewModel.FileExtension FileExtension { get; } = new IBaseExporterConfiguratorViewModel.FileExtension("CSV", "csv");


        private readonly ICSVEvaluationExporterFactory csvEvaluationExporterFactory;

        private CSVExporterConfiguratorViewModel(ICSVEvaluationExporterFactory csvEvaluationExporterFactory)
        {
            this.csvEvaluationExporterFactory = csvEvaluationExporterFactory;
            Path = "export.csv";
        }

        public override string CreateConfig()
        {
            var config = new CSVEvaluationExporterFactory.Config
            {
                Path = Path,
                ExportValue = ExportValue,
                ExportGroupKey = ExportGroupKey,
                ExportVariables = ExportVariables
            };
            if (IsExportVariablesFilterEnabled)
            {
                config.ExportVariablesFilter = ExportVariablesFilter.Select(x => x.Variable).OfType<string>().ToHashSet();
            }
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        protected override bool DoTryLoadConfig(string config)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<CSVEvaluationExporterFactory.Config>(config);

                if (cfg != null)
                {
                    Path = cfg.Path;
                    ExportValue = cfg.ExportValue;
                    ExportGroupKey = cfg.ExportGroupKey;
                    ExportVariables = cfg.ExportVariables;

                    ClearExportVariablesFilters();
                    foreach (var filter in cfg.ExportVariablesFilter)
                    {
                        AddExportVariablesFilter(filter);
                    }
                    IsExportVariablesFilterEnabled = cfg.ExportVariablesFilter.Any();

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
            return csvEvaluationExporterFactory.Create(ctx, CreateConfig());
        }
    }
}
