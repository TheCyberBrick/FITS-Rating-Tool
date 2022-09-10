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
using Newtonsoft.Json;
using nom.tam.fits;

namespace FitsRatingTool.Exporters.Services.Impl
{
    public class FitsHeaderEvaluationExporterFactory : IFitsHeaderEvaluationExporterFactory
    {
        public class Config
        {
            [JsonProperty(PropertyName = "keyword", Required = Required.Always)]
            public string Keyword { get; set; } = null!;

            [JsonProperty(PropertyName = "export_value", NullValueHandling = NullValueHandling.Ignore)]
            public bool ExportValue { get; set; } = true;
        }

        private class Exporter : IEvaluationExporter
        {
            public string? ConfirmationMessage => null;

            public bool ExportValue { get; set; }

            public bool CanExportGroupKey => false;

            public bool ExportGroupKey { get => false; set { } }

            public bool CanExportVariables => false;

            public bool ExportVariables { get => false; set { } }

            public ISet<string> ExportVariablesFilter => new HashSet<string>();


            private readonly string keyword;

            public Exporter(string keyword)
            {
                keyword = keyword.ToUpper();
                if (keyword.Length == 0 || keyword.Length > 8 || keyword.Any(c => c >= 128))
                {
                    throw new ArgumentException("Invalid keyword '" + keyword + "'");
                }
                this.keyword = keyword;
            }


            public async Task ExportAsync(IEvaluationExporterEventDispatcher events, string file, string groupKey, IEnumerable<KeyValuePair<string, double>> variableValues, double value, CancellationToken cancellationToken = default)
            {
                if (ExportValue)
                {
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);

                    var fits = new Fits(fs);

                    try
                    {
                        fits.Read();

                        BasicHDU hdu = fits.GetHDU(0);

                        Header header = hdu.Header;

                        header.AddValue(keyword, value, "Fits Rating Tool Rating");

                        header.Rewrite();

                        await fs.FlushAsync(cancellationToken);
                    }
                    finally
                    {
                        fits.Close();
                    }
                }
            }

            public Task FlushAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void Close()
            {
            }

            public void Dispose()
            {
            }
        }

        public string Description => "Exports the evaluations into the corresponding FITs headers";

        public string ExampleConfig => JsonConvert.SerializeObject(new Config()
        {
            Keyword = "MYKEYWRD",
            ExportValue = true
        }, Formatting.Indented);

        public IEvaluationExporter Create(IEvaluationExporterContext ctx, string config)
        {
            var cfg = JsonConvert.DeserializeObject<Config>(config);
            if (cfg == null)
            {
                throw new ArgumentException("Invalid config");
            }
            return new Exporter(cfg.Keyword)
            {
                ExportValue = cfg.ExportValue
            };
        }
    }
}
