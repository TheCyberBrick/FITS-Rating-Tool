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
using System.Text;

namespace FitsRatingTool.Exporters.Services.Impl
{
    public class FileMoverExporterFactory : IFileMoverExporterFactory
    {
        public class Config
        {

            [JsonProperty(PropertyName = "max_rating_threshold", NullValueHandling = NullValueHandling.Ignore)]
            public float? MaxRatingThreshold { get; set; }

            [JsonProperty(PropertyName = "min_rating_threshold", NullValueHandling = NullValueHandling.Ignore)]
            public float? MinRatingThreshold { get; set; }

            [JsonProperty(PropertyName = "path", Required = Required.Always)]
            public string Path { get; set; } = null!;

            [JsonProperty(PropertyName = "is_relative_path", Required = Required.Always)]
            public bool IsRelativePath { get; set; } = false;

            [JsonProperty(PropertyName = "parent_dirs", NullValueHandling = NullValueHandling.Ignore)]
            public int ParentDirs { get; set; } = 0;
        }

        private class Exporter : IEvaluationExporter
        {
            public string? ConfirmationMessage => "This exporter moves files to another directory if their rating is below or above a certain threshold. File name conflicts are resolved by appending a number to the conflicting file name. Please make sure that this exporter is configured correctly and before running it, double check that the correct files are loaded and/or that the job file using this exporter points to the correct files.";

            public bool ExportValue { get => false; set { } }

            public bool CanExportGroupKey => false;

            public bool ExportGroupKey { get => false; set { } }

            public bool CanExportVariables => false;

            public bool ExportVariables { get => false; set { } }

            public ISet<string> ExportVariablesFilter => throw new NotImplementedException();

            private readonly AsyncSemaphore nameSemaphore = new(1);
            private readonly AsyncSemaphore moveSemaphore = new(8);

            private readonly HashSet<string> reservedFiles = new();

            private readonly Config config;

            public Exporter(Config config)
            {
                this.config = config;
            }

            private bool IsAvailable(string file)
            {
                return !File.Exists(file) && !reservedFiles.Contains(file);
            }

            private string GetParentPath(string file, int parentDirs)
            {
                List<string> dirs = new();

                for (int i = 0; i < parentDirs; i++)
                {
                    var parent = Directory.GetParent(i == 0 ? file : dirs.Last());
                    if (parent != null)
                    {
                        dirs.Add(parent.FullName);
                    }
                    else
                    {
                        break;
                    }
                }

                if (dirs.Count > 0)
                {
                    StringBuilder sb = new();

                    for (int i = dirs.Count - 1; i >= 0; --i)
                    {
                        var name = Path.GetFileName(dirs[i]);
                        if (name.Length != 0)
                        {
                            sb.Append(name);
                            if (i != 0) sb.Append(Path.DirectorySeparatorChar);
                        }
                    }

                    return sb.ToString();
                }

                return string.Empty;
            }

            private string GetTargetPath(string file)
            {
                string basePath = config.Path;

                if (!basePath.EndsWith(Path.DirectorySeparatorChar))
                {
                    basePath = basePath + Path.DirectorySeparatorChar;
                }

                if (config.IsRelativePath)
                {
                    while (basePath.StartsWith(Path.DirectorySeparatorChar))
                    {
                        basePath = basePath[1..];
                    }

                    var fileDir = Path.GetDirectoryName(file) ?? throw new InvalidOperationException("Path '" + file + "' is not a file");
                    basePath = fileDir + Path.DirectorySeparatorChar + basePath;
                }

                if (config.ParentDirs > 0)
                {
                    string parentPath = GetParentPath(file, config.ParentDirs);

                    if (parentPath.Length > 0)
                    {
                        basePath = basePath + parentPath + Path.DirectorySeparatorChar;
                    }
                }

                return basePath + Path.GetFileName(file);
            }

            public async Task ExportAsync(IEvaluationExporterEventDispatcher events, string file, string groupKey, IEnumerable<KeyValuePair<string, double>> variableValues, double value, CancellationToken cancellationToken = default)
            {
                if ((config.MaxRatingThreshold.HasValue && value > config.MaxRatingThreshold.Value) || (config.MinRatingThreshold.HasValue && value < config.MinRatingThreshold.Value))
                {
                    string oldFile = file;

                    if (File.Exists(oldFile))
                    {
                        string targetPath = GetTargetPath(oldFile);

                        var parentPath = Directory.GetParent(targetPath);
                        if (parentPath != null)
                        {
                            Directory.CreateDirectory(parentPath.FullName);
                        }

                        string? newFile = null;

                        Exception? ex = null;

                        bool success;

                        using (await moveSemaphore.EnterAsync(cancellationToken))
                        {
                            success = await Task.Run(async () =>
                            {
                                const int attempts = 10;
                                for (int i = 0; i < attempts; ++i)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    newFile = targetPath;

                                    using (await nameSemaphore.EnterAsync(cancellationToken))
                                    {
                                        if (!IsAvailable(newFile))
                                        {
                                            string fileDir = Path.GetDirectoryName(newFile) ?? throw new InvalidOperationException("Path '" + newFile + "' is not a file");
                                            string fileName = Path.GetFileNameWithoutExtension(newFile);
                                            string fileExt = Path.GetExtension(newFile);

                                            uint nr = 0;

                                            do
                                            {
                                                cancellationToken.ThrowIfCancellationRequested();
                                                ++nr;
                                                newFile = Path.Combine(fileDir, string.Format("{0}_{1:0000}{2}", fileName, nr, fileExt));
                                            } while (!IsAvailable(newFile) && nr < 9999);

                                            if (!IsAvailable(newFile))
                                            {
                                                newFile = null;
                                            }
                                        }

                                        if (newFile != null)
                                        {
                                            reservedFiles.Add(newFile);
                                        }
                                    }

                                    try
                                    {
                                        if (newFile != null)
                                        {
                                            events.Send(this, IFileMoverExporterFactory.EVENT_MOVING_FILE, new IFileMoverExporterFactory.MovingFileEventParameters(oldFile, newFile));

                                            File.Move(oldFile, newFile, false);

                                            return true;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        // Try again
                                        ex = e;
                                    }
                                    finally
                                    {
                                        if (newFile != null)
                                        {
                                            reservedFiles.Remove(newFile);
                                        }
                                    }

                                    await Task.Delay(1000, cancellationToken);
                                }

                                return false;
                            });
                        }

                        if (!success || newFile == null)
                        {
                            throw new IOException("Unable to delete file '" + oldFile + "'", ex);
                        }

                        events.Send(this, IFileMoverExporterFactory.EVENT_MOVED_FILE, new IFileMoverExporterFactory.MovingFileEventParameters(oldFile, newFile));
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
                nameSemaphore.Dispose();
                moveSemaphore.Dispose();
            }
        }

        public string Description => "Moves files to another directory if their rating is below a minimum" + Environment.NewLine + "rating threshold or above a maximum rating threshold";

        public string ExampleConfig =>
            JsonConvert.SerializeObject(new Config()
            {
                MinRatingThreshold = 6,
                IsRelativePath = true,
                Path = "rejected"
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
