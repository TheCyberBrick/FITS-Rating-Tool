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
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;

namespace FitsRatingTool.Exporters.Services.Impl
{
    public class CSVEvaluationExporterFactory : ICSVEvaluationExporterFactory
    {
        public class Config
        {
            [JsonProperty(PropertyName = "path", Required = Required.Always)]
            public string Path { get; set; } = null!;

            [JsonProperty(PropertyName = "export_value", NullValueHandling = NullValueHandling.Ignore)]
            public bool ExportValue { get; set; } = true;

            [JsonProperty(PropertyName = "export_group_key", NullValueHandling = NullValueHandling.Ignore)]
            public bool ExportGroupKey { get; set; } = true;

            [JsonProperty(PropertyName = "export_variables", NullValueHandling = NullValueHandling.Ignore)]
            public bool ExportVariables { get; set; } = false;

            [JsonProperty(PropertyName = "export_variables_filter", NullValueHandling = NullValueHandling.Ignore)]
            public ISet<string> ExportVariablesFilter { get; set; } = new HashSet<string>();
        }

        private class Exporter : IEvaluationExporter
        {
            public string? ConfirmationMessage => null;

            public bool RequiresConfirmation => true;

            public bool ExportValue
            {
                get => config.ExportValue;
                set => config.ExportValue = value;
            }

            public bool CanExportGroupKey => true;

            public bool ExportGroupKey
            {
                get => config.ExportGroupKey;
                set => config.ExportGroupKey = value;
            }

            public bool CanExportVariables => true;

            public bool ExportVariables
            {
                get => config.ExportVariables;
                set => config.ExportVariables = value;
            }

            public ISet<string> ExportVariablesFilter => config.ExportVariablesFilter;

            private readonly Config config;
            private readonly string csvPath;

            public Exporter(IEvaluationExporterContext ctx, Config config)
            {
                this.config = config;
                csvPath = ctx.ResolvePath(config.Path);
            }

            private StreamWriter? writer = null;
            private AsyncSemaphore writerSemaphore = new(1);
            private bool headerWritten = false;

            private volatile bool disposed = false;

            private int counter = 0;

            private void WriteHeader(StreamWriter writer, IEnumerable<KeyValuePair<string, double>> variableValues)
            {
                StringBuilder header = new();

                header.Append("File,");

                if (ExportValue)
                {
                    header.Append("Value,");
                }

                if (ExportGroupKey)
                {
                    header.Append("GroupKey,");
                }

                if (ExportVariables)
                {
                    foreach (var entry in variableValues)
                    {
                        if (ExportVariablesFilter.Count == 0 || ExportVariablesFilter.Contains(entry.Key))
                        {
                            header.Append(entry.Key);
                            header.Append(',');
                        }
                    }
                }

                --header.Length;

                writer.WriteLine(header);
            }

            public async Task ExportAsync(IEvaluationExporterEventDispatcher events, string file, string groupKey, IEnumerable<KeyValuePair<string, double>> variableValues, double value, CancellationToken cancellationToken = default)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(Exporter));
                }

                lock (this)
                {
                    if (writer == null)
                    {
                        writer = new StreamWriter(csvPath);
                        headerWritten = false;
                    }

                    if (!headerWritten)
                    {
                        WriteHeader(writer, variableValues);
                        headerWritten = true;
                    }
                }

                StringBuilder record = new();

                void append(string val)
                {
                    record.Append('"');
                    record.Append(val);
                    record.Append("\",");
                }

                append(file);

                if (ExportValue)
                {
                    append(value.ToString("0.######", CultureInfo.InvariantCulture));
                }

                if (ExportGroupKey)
                {
                    append(groupKey);
                }

                if (ExportVariables)
                {
                    foreach (var entry in variableValues)
                    {
                        if (ExportVariablesFilter.Count == 0 || ExportVariablesFilter.Contains(entry.Key))
                        {
                            append(entry.Value.ToString("0.######", CultureInfo.InvariantCulture));
                        }
                    }
                }

                --record.Length;

                using (await writerSemaphore.EnterAsync(cancellationToken))
                {
                    await writer.WriteLineAsync(record, cancellationToken);

                    if (++counter >= 100)
                    {
                        counter = 0;
                        await FlushUnsafeAsync();
                    }
                }
            }

            private async Task FlushUnsafeAsync()
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(Exporter));
                }

                var wr = writer;
                if (wr != null)
                {
                    await wr.FlushAsync();
                }
            }

            public async Task FlushAsync(CancellationToken cancellationToken = default)
            {
                using (await writerSemaphore.EnterAsync(cancellationToken))
                {
                    await FlushUnsafeAsync();
                }
            }

            public void Close()
            {
                Dispose();
            }

            public void Dispose()
            {
                writer?.Dispose();
                writer = null;
                disposed = true;
            }
        }

        public string Description => "Exports the evaluations into a CSV file";

        public string ExampleConfig => JsonConvert.SerializeObject(new Config()
        {
            Path = "C:/My/Path/To/File.csv",
            ExportValue = true,
            ExportGroupKey = true,
            ExportVariables = true,
            ExportVariablesFilter = { "MyVariable1", "MyVariable2" }
        }, Formatting.Indented);

        public IEvaluationExporter Create(IEvaluationExporterContext ctx, string config)
        {
            var cfg = JsonConvert.DeserializeObject<Config>(config);
            if (cfg == null)
            {
                throw new ArgumentException("Invalid config");
            }
            return new Exporter(ctx, cfg);
        }
    }
}
