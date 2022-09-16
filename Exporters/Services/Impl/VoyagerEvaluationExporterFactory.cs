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
using Newtonsoft.Json.Linq;
using nom.tam.fits;
using System.Net.Sockets;
using VoyagerAPI.Runner;

namespace FitsRatingTool.Exporters.Services.Impl
{
    public class VoyagerEvaluationExporterFactory : IVoyagerEvaluationExporterFactory
    {
        public class Config
        {
            [JsonProperty(PropertyName = "application_server_hostname", NullValueHandling = NullValueHandling.Ignore)]
            public string ApplicationServerHostname { get; set; } = "localhost";

            [JsonProperty(PropertyName = "application_server_port", NullValueHandling = NullValueHandling.Ignore)]
            public int ApplicationServerPort { get; set; } = 5950;

            [JsonProperty(PropertyName = "credentials_file", NullValueHandling = NullValueHandling.Ignore)]
            public string CredentialsFile { get; set; } = "%userprofile%/voyager_credentials.json";

            [JsonProperty(PropertyName = "max_rating_threshold", NullValueHandling = NullValueHandling.Ignore)]
            public int? MaxRatingThreshold { get; set; }

            [JsonProperty(PropertyName = "min_rating_threshold", NullValueHandling = NullValueHandling.Ignore)]
            public int? MinRatingThreshold { get; set; }

            [JsonProperty(PropertyName = "reset_ratings_and_restore", NullValueHandling = NullValueHandling.Ignore)]
            public bool ResetRatingsAndRestore { get; set; }
        }

        public class Credentials : IVoyagerEvaluationExporterFactory.IVoyagerCredentials
        {
            [JsonProperty(PropertyName = "application_server_username", NullValueHandling = NullValueHandling.Ignore)]
            public string? ApplicationServerUsername { get; set; }

            [JsonProperty(PropertyName = "application_server_password", NullValueHandling = NullValueHandling.Ignore)]
            public string? ApplicationServerPassword { get; set; }

            [JsonProperty(PropertyName = "robotarget_secret", NullValueHandling = NullValueHandling.Ignore)]
            public string? RoboTargetSecret { get; set; }
        }

        private class Module : PassiveRunnerModule
        {
            public override string Name => "Voyager Evaluation Exporter";
        }

        private class Exporter : IEvaluationExporter
        {
            public string? ConfirmationMessage => null;

            public bool ExportValue { get; set; } = true;

            public bool CanExportGroupKey => false;

            public bool ExportGroupKey { get => false; set { } }

            public bool CanExportVariables => false;

            public bool ExportVariables { get => false; set { } }

            public ISet<string> ExportVariablesFilter => new HashSet<string>();


            private readonly AsyncSemaphore writeSemaphore = new(1);
            private readonly Dictionary<string, Tuple<int, bool>> writeBuffer = new();
            private int counter = 0;

            private readonly AsyncSemaphore initSemaphore = new(1);
            private Runner? runner;
            private Module? module;

            private volatile bool disposed;


            private readonly Config config;
            private readonly Credentials credentials;

            public Exporter(Config config, Credentials credentials)
            {
                this.config = config;
                this.credentials = credentials;
            }

            private async Task InitRunnerAsync(CancellationToken cancellationToken = default)
            {
                using (await initSemaphore.EnterAsync(cancellationToken))
                {
                    if (runner == null)
                    {
                        bool requiresAuthentication = credentials.ApplicationServerUsername != null || credentials.ApplicationServerPassword != null;

                        runner = new Runner(() => config.ApplicationServerHostname, () => config.ApplicationServerPort,
                            requiresAuthentication ? new Runner.Credentials() { Username = credentials.ApplicationServerUsername ?? "", Password = credentials.ApplicationServerPassword ?? "" } : null,
                            credentials.RoboTargetSecret != null ? new Runner.Credentials() { Password = credentials.RoboTargetSecret } : null,
                            m => new SilentRunnerLog());

                        module = new Module();

                        runner.AddModule(module);

                        var tcs = new TaskCompletionSource<bool>();

                        // Listen for state changes in order to
                        // detect a successfull connection and/or
                        // authentication
                        void onStateChanged(object? sender, Runner.RunnerState state)
                        {
                            if ((state == Runner.RunnerState.Connected && !requiresAuthentication) || (state == Runner.RunnerState.Authenticated && requiresAuthentication))
                            {
                                try
                                {
                                    tcs.TrySetResult(true);
                                }
                                catch (ObjectDisposedException)
                                {
                                }
                            }
                        }
                        runner.OnStateChange += onStateChanged;

                        var runnerTask = RunAsync(runner, tcs, cancellationToken);

                        // Wait for successful connection or failure
                        var success = await tcs.Task;

                        runner.OnStateChange -= onStateChanged;

                        if (!success)
                        {
                            try
                            {
                                await runner.ShutdownAsync();
                            }
                            catch (Exception)
                            {
                            }

                            try
                            {
                                // Await the runner task here to throw
                                // the exception that caused the failure
                                await runnerTask;

                                throw new Exception("Unable to connect to Voyager");
                            }
                            finally
                            {
                                Dispose();
                            }
                        }
                    }
                }
            }

            private static async Task RunAsync(Runner runner, TaskCompletionSource<bool> tcs, CancellationToken cancellationToken = default)
            {
                try
                {
                    int attempt = 0;

                    while (!runner.IsShutdown)
                    {
                        try
                        {
                            await runner.RunAsync(cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                            // Retry connection for 3 times before
                            // it gives up
                            if (++attempt >= 3)
                            {
                                throw;
                            }
                        }

                        // Wait for 2s before the next attempt
                        await Task.Delay(2000, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Runner was stopped
                    tcs.SetCanceled();
                }
                finally
                {
                    try
                    {
                        // Failed to connect, return failure
                        tcs.TrySetResult(false);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }

            private static string? ReadShotGUID(string file)
            {
                string? shotGuid;

                using (var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
                {
                    var fits = new Fits(fs);

                    try
                    {
                        fits.Read();

                        BasicHDU hdu = fits.GetHDU(0);

                        Header header = hdu.Header;

                        shotGuid = header.GetStringValue("RTSHOT");
                    }
                    finally
                    {
                        fits.Close();
                    }
                }

                return shotGuid;
            }

            private bool ShouldDeleteOrRestore()
            {
                return config.ResetRatingsAndRestore || config.MinRatingThreshold.HasValue || config.MaxRatingThreshold.HasValue;
            }

            public async Task ExportAsync(IEvaluationExporterEventDispatcher events, string file, string groupKey, IEnumerable<KeyValuePair<string, double>> variableValues, double value, CancellationToken cancellationToken = default)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(Exporter));
                }

                if (ExportValue)
                {
                    await InitRunnerAsync(cancellationToken);

                    var shotGuid = ReadShotGUID(file);

                    if (shotGuid == null)
                    {
                        throw new Exception("FITS file header does not contain RTSHOT header string (= RoboTarget Shot UID)");
                    }

                    // Voyager considers a rating of <= 0 as having no rating,
                    // thus + 1 is added
                    var adjustedRating = config.ResetRatingsAndRestore ? 0 : 1 + (int)Math.Round(Math.Max(0, value));

                    bool delete = false;
                    if (!config.ResetRatingsAndRestore)
                    {
                        if (config.MaxRatingThreshold.HasValue && adjustedRating > config.MaxRatingThreshold.Value)
                        {
                            delete = true;
                        }
                        else if (config.MinRatingThreshold.HasValue && adjustedRating < config.MinRatingThreshold.Value)
                        {
                            delete = true;
                        }
                    }

                    using (await writeSemaphore.EnterAsync(cancellationToken))
                    {
                        lock (writeBuffer)
                        {
                            writeBuffer.Add(shotGuid, Tuple.Create(adjustedRating, delete));
                        }

                        if (++counter >= 50)
                        {
                            counter = 0;
                            await FlushAsync(cancellationToken);
                        }
                    }
                }
            }

            private async Task SendBulkUpdateCommandAsync(JObject args, CancellationToken cancellationToken = default)
            {
                var connection = module?.Connection;

                if (connection == null)
                {
                    throw new Exception("Not connected to Voyager");
                }

                var result = await connection.WithMAC().SendCommandAsync("RemoteOpenRoboTargetUpdateBulkShotDone", args, true, cancellationToken);

                if (result.errorMessage != null)
                {
                    throw new Exception("Failed updating RoboTarget database: " + result.errorMessage);
                }
            }

            public async Task FlushAsync(CancellationToken cancellationToken = default)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(Exporter));
                }

                bool empty = true;

                JArray srcListRestoreAll = new();
                JArray srcListUpdateAll = new();
                JArray srcList = new();

                lock (writeBuffer)
                {
                    foreach (var entry in writeBuffer)
                    {
                        var shotGuid = entry.Key;
                        var rating = entry.Value.Item1;
                        var delete = entry.Value.Item2;

                        // Set IsToDelete to true so that the shot
                        // can be restored with IsDeleted = true
                        // further down
                        srcListRestoreAll.Add(new JObject()
                        {
                            new JProperty("RefGuidShotDone", shotGuid),
                            new JProperty("Rating", 0),
                            new JProperty("IsToDelete", true)
                        });

                        // Set IsToDelete to whether the shot should
                        // actually be deleted so that the shots
                        // can be deleted if necessary further down
                        srcList.Add(new JObject()
                        {
                            new JProperty("RefGuidShotDone", shotGuid),
                            new JProperty("Rating", 0),
                            new JProperty("IsToDelete", delete)
                        });

                        // Set IsToDelete to false so that the shot
                        // rating can be updated
                        srcListUpdateAll.Add(new JObject()
                        {
                            new JProperty("RefGuidShotDone", shotGuid),
                            new JProperty("Rating", rating),
                            new JProperty("IsToDelete", false)
                        });

                        empty = false;
                    }
                    writeBuffer.Clear();
                }

                if (!empty)
                {
                    if (ShouldDeleteOrRestore())
                    {
                        // First make sure all shots are restored
                        await SendBulkUpdateCommandAsync(new()
                        {
                            new JProperty("SrcList", srcListRestoreAll),
                            new JProperty("IsDeleted", true)
                        }, cancellationToken);

                        // Then make sure all shots where thresholds are
                        // exceeded are deleted
                        await SendBulkUpdateCommandAsync(new()
                        {
                            new JProperty("SrcList", srcList),
                            new JProperty("IsDeleted", false)
                        }, cancellationToken);
                    }

                    // And lastly update the ratings of all shots
                    await SendBulkUpdateCommandAsync(new()
                    {
                        new JProperty("SrcList", srcListUpdateAll),
                        new JProperty("IsDeleted", false)
                    }, cancellationToken);
                }
            }

            public void Close()
            {
                Dispose();
            }

            public void Dispose()
            {
                runner?.Dispose();
                runner = null;
                writeSemaphore.Dispose();
                initSemaphore.Dispose();
                disposed = true;
            }
        }

        public string Description => "Exports the ratings into the Voyager RoboTarget database" + Environment.NewLine + "and optionally deletes/restores the corresponding shots" + Environment.NewLine + "in the database (files are never deleted!) according" + Environment.NewLine + "to a min. or max. rating threshold";

        public string ExampleConfig =>
            JsonConvert.SerializeObject(new Config()
            {
                MinRatingThreshold = 6
            }, Formatting.Indented) + Environment.NewLine + Environment.NewLine +
            "...and example credentials %userprofile%/voyager_credentials.json:" + Environment.NewLine +
            SaveCredentials(new Credentials()
            {
                ApplicationServerUsername = "username",
                ApplicationServerPassword = "password",
                RoboTargetSecret = "secret"
            });

        public IEvaluationExporter Create(IEvaluationExporterContext ctx, string config)
        {
            var cfg = JsonConvert.DeserializeObject<Config>(config);
            if (cfg == null)
            {
                throw new ArgumentException("Invalid config");
            }
            var credsFile = ctx.ResolvePath(cfg.CredentialsFile);
            Credentials? creds = null;
            try
            {
                creds = LoadCredentials(File.ReadAllText(credsFile)) as Credentials;
            }
            catch (Exception)
            {
            }
            if (creds == null)
            {
                throw new ArgumentException("Invalid credentials file '" + credsFile + "'");
            }
            return new Exporter(cfg, creds);
        }

        public IVoyagerEvaluationExporterFactory.IVoyagerCredentials CreateCredentials()
        {
            return new Credentials();
        }

        public IVoyagerEvaluationExporterFactory.IVoyagerCredentials LoadCredentials(string data)
        {
            var creds = JsonConvert.DeserializeObject<Credentials>(data);
            if (creds == null)
            {
                throw new ArgumentException("Invalid credentials file");
            }
            return creds;
        }

        public string SaveCredentials(IVoyagerEvaluationExporterFactory.IVoyagerCredentials credentials)
        {
            return JsonConvert.SerializeObject(credentials, Formatting.Indented);
        }
    }
}
