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

namespace FitsRatingTool.Exporters.Services.Impl
{
    public class FileDeleterExporterFactory : IFileDeleterExporterFactory
    {
        public class Config
        {

            [JsonProperty(PropertyName = "max_rating_threshold", NullValueHandling = NullValueHandling.Ignore)]
            public float? MaxRatingThreshold { get; set; }

            [JsonProperty(PropertyName = "min_rating_threshold", NullValueHandling = NullValueHandling.Ignore)]
            public float? MinRatingThreshold { get; set; }
        }

        private class Exporter : IEvaluationExporter
        {
            public string? ConfirmationMessage => "This exporter permanently deletes files if their rating is below or above a certain threshold! Please make sure that this exporter is configured correctly and before running it, double check that the correct files are loaded and/or that the job file using this exporter points to the correct files.";

            public bool ExportValue { get => false; set { } }

            public bool CanExportGroupKey => false;

            public bool ExportGroupKey { get => false; set { } }

            public bool CanExportVariables => false;

            public bool ExportVariables { get => false; set { } }

            public ISet<string> ExportVariablesFilter => throw new NotImplementedException();


            private readonly AsyncSemaphore deleteSemaphore = new AsyncSemaphore(8);

            private readonly Config config;

            public Exporter(Config config)
            {
                this.config = config;
            }

            public async Task ExportAsync(IEvaluationExporterEventDispatcher events, string file, string groupKey, IEnumerable<KeyValuePair<string, double>> variableValues, double value, CancellationToken cancellationToken = default)
            {
                if ((config.MaxRatingThreshold.HasValue && value > config.MaxRatingThreshold.Value) || (config.MinRatingThreshold.HasValue && value < config.MinRatingThreshold.Value))
                {
                    events.Send(this, IFileDeleterExporterFactory.EVENT_DELETING_FILE, new IFileDeleterExporterFactory.DeletingFileEventParameters(file));

                    var fi = new FileInfo(file);

                    if (fi.Exists)
                    {
                        Exception? ex = null;

                        void attemptDelete()
                        {
                            try
                            {
                                fi.Delete();
                            }
                            catch (Exception e)
                            {
                                // Try again
                                ex = e;
                            }

                            try
                            {
                                fi.Refresh();
                            }
                            catch (Exception)
                            {
                            }
                        }

                        attemptDelete();

                        if (fi.Exists)
                        {
                            using (await deleteSemaphore.EnterAsync(cancellationToken))
                            {
                                await Task.Run(async () =>
                                {
                                    const int attempts = 10;
                                    for (int i = 0; i < attempts && fi.Exists; ++i)
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();

                                        attemptDelete();

                                        await Task.Delay(1000, cancellationToken);
                                    }
                                });
                            }
                        }

                        if (fi.Exists)
                        {
                            throw new IOException("Unable to delete file '" + file + "'", ex);
                        }
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
                deleteSemaphore.Dispose();
            }
        }

        public string Description => "Permanently (!) deletes files if their rating is below a minimum" + Environment.NewLine + "rating threshold or above a maximum rating threshold";

        public string ExampleConfig =>
            JsonConvert.SerializeObject(new Config()
            {
                MinRatingThreshold = 6
            }, Formatting.Indented);

        public IEvaluationExporter Create(IEvaluationExporterContext ctx, string config)
        {
            var cfg = JsonConvert.DeserializeObject<Config>(config);
            if (cfg == null)
            {
                throw new ArgumentException("Invalid config");
            }
            return new Exporter(cfg);
        }
    }
}
