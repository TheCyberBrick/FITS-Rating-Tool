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
using FitsRatingTool.Common.Services;
using FitsRatingTool.Common.Utils;
using FitsRatingTool.Exporters.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace FitsRatingTool.ConsoleApp
{
    public class ConsoleApp
    {
        private class ProgressStatus
        {
            public bool progressWritten;
            public bool done;
        }

        private readonly IJobConfigFactory jobConfigFactory;
        private readonly IStandaloneEvaluationService evaluator;

        private Dictionary<string, IEvaluationExporterFactory> exporterFactories = new();

        public ConsoleApp(IJobConfigFactory jobConfigFactory, IStandaloneEvaluationService evaluator, ICSVEvaluationExporterFactory csvEvaluationExporterFactory,
            IFitsHeaderEvaluationExporterFactory fitsHeaderEvaluationExporterFactory, IVoyagerEvaluationExporterFactory voyagerEvaluationExporterFactory,
            IFileDeleterExporterFactory fileDeleterExporterFactory, IFileMoverExporterFactory fileMoverExporterFactory)
        {
            this.jobConfigFactory = jobConfigFactory;
            this.evaluator = evaluator;

            RegisterExporter("csv", csvEvaluationExporterFactory);
            RegisterExporter("fits_header", fitsHeaderEvaluationExporterFactory);
            RegisterExporter("voyager", voyagerEvaluationExporterFactory);
            RegisterExporter("file_deleter", fileDeleterExporterFactory);
            RegisterExporter("file_mover", fileMoverExporterFactory);
        }

        private void RegisterExporter(string id, IEvaluationExporterFactory factory)
        {
            if (evaluator.RegisterExporter(id, (ctx, conf) => factory.Create(ctx, conf)))
            {
                exporterFactories.Add(id, factory);
            }
        }

        private string PrintWithSpacing(int spacing, string text)
        {
            var lines = text.Split(Environment.NewLine);
            StringBuilder str = new();
            foreach (var line in lines)
            {
                for (int i = 0; i < spacing; i++)
                {
                    str.Append(" ");
                }
                str.AppendLine(line);
            }
            if (str.Length > 0)
            {
                --str.Length;
            }
            return str.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002")]
        public int Run(string[] args)
        {
            string? GetArgument(IEnumerable<string> args, string? shortOption, string option) => args.SkipWhile(i => (shortOption == null || !i.Equals(shortOption)) && !i.Equals(option)).Skip(1).Take(1).FirstOrDefault();

            bool informationOnly = false;

            if (args.Contains("-e") || args.Contains("--exporters"))
            {
                Console.WriteLine("Exporters:");
                Console.WriteLine();
                foreach (var exporterId in evaluator.Exporters)
                {
                    var exporterFactory = exporterFactories[exporterId];
                    Console.WriteLine(" - Id: " + exporterId);
                    Console.WriteLine("   Description: " + Environment.NewLine + PrintWithSpacing(5, exporterFactory.Description));
                    Console.WriteLine("   Example config: " + Environment.NewLine + PrintWithSpacing(5, exporterFactory.ExampleConfig));
                    Console.WriteLine();
                }
                informationOnly = true;
            }

            if (args.Contains("-h") || args.Contains("--help"))
            {
                PrintUsage(false);
                informationOnly = true;
            }

            if (informationOnly)
            {
                return 0;
            }

            var yesFlag = args.Contains("-y") || args.Contains("--yes");
            var wipeCacheFlag = args.Contains("-c") || args.Contains("--clearcache");
            var outputFlag = args.Contains("-o") || args.Contains("--output");
            var silentFlag = args.Contains("-s") || args.Contains("--silent");
            var progressFlag = args.Contains("--progress");
            var debugFlag = args.Contains("-d") || args.Contains("--debug");
            var initFlag = args.Contains("-i") || args.Contains("--init");

            var jobConfigFile = GetArgument(args, "-j", "--job");
            var path = GetArgument(args, "-p", "--path");
            var filePattern = GetArgument(args, "-f", "--filepattern");

            void printException(Exception e)
            {
                if (e is AggregateException ae)
                {
                    ae.Flatten().Handle(ex =>
                    {
                        printException(ex);
                        return true;
                    });
                }
                else if (e is OperationCanceledException)
                {
                    Console.Error.WriteLine("Evaluation was cancelled");
                    if (debugFlag)
                    {
                        Console.Error.WriteLine(e);
                    }
                }
                else if (e is IJobConfigFactory.InvalidJobConfigException || e is IBatchEvaluationService.InvalidConfigException ||
                    e is IStandaloneEvaluationService.InvalidConfigException || e is FileNotFoundException)
                {
                    Console.Error.WriteLine(debugFlag ? e : e.Message);
                }
                else
                {
                    Console.Error.WriteLine(e);
                }
            };

            if (jobConfigFile != null && (path != null || initFlag || wipeCacheFlag))
            {
                if (initFlag && !File.Exists(jobConfigFile))
                {
                    var newConfig = jobConfigFactory.Builder().Build();

                    newConfig.EvaluationFormula = "# Define a custom variable" + Environment.NewLine + "Rating := -FWHMSigma;" + Environment.NewLine + Environment.NewLine + "# Last expression returns the evaluation result" + Environment.NewLine + "Rating";
                    newConfig.ParallelTasks = 4;
                    newConfig.GroupingKeys = new HashSet<string>() { "Object", "Filter" };
                    newConfig.GroupingKeysRequired = true;
                    newConfig.GroupingFilters = new HashSet<IReadOnlyJobConfig.FilterConfig>() { new IReadOnlyJobConfig.FilterConfig("Object", "\\S+") };
                    newConfig.OutputLogsPath = "logs";
                    newConfig.CachePath = "cache";
                    newConfig.MaxImageSize = 805306368;
                    newConfig.MaxImageWidth = 8192;
                    newConfig.MaxImageHeight = 8192;
                    newConfig.Exporters = new List<IReadOnlyJobConfig.ExporterConfig>()
                    {
                        new IReadOnlyJobConfig.ExporterConfig("csv", @"
{
  ""path"": ""export.csv"",
  ""export_value"": true,
  ""export_group_key"": true,
  ""export_variables"": true,
  ""export_variables_filter"": [
    ""Rating"",
    ""FWHMMin"",
    ""FWHMMax"",
    ""FWHMMedian"",
    ""FWHMSigma""
  ]
}
")
                    };

                    try
                    {
                        File.WriteAllText(jobConfigFile, jobConfigFactory.Save(newConfig));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Could not initialize new job config file:");
                        printException(ex);
                        return 1;
                    }
                }

                if (wipeCacheFlag)
                {
                    string? cachePath;
                    try
                    {
                        cachePath = evaluator.GetCachePath(jobConfigFile);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Cannot clear cache because the job config file could not be read:");
                        printException(ex);
                        return 1;
                    }

                    if (cachePath != null)
                    {
                        if (!yesFlag)
                        {
                            while (true)
                            {
                                Console.Write("Are you sure you want to clear the cache at '" + cachePath + "'? [y/n] ");
                                var input = Console.ReadKey(false).Key;
                                if (input != ConsoleKey.Enter) Console.WriteLine();
                                if (input == ConsoleKey.Y)
                                {
                                    break;
                                }
                                else if (input == ConsoleKey.N)
                                {
                                    return 0;
                                }
                            }
                        }

                        try
                        {
                            if (!silentFlag)
                            {
                                Console.WriteLine("Clearing cache...");
                            }

                            int n = evaluator.ClearCache(jobConfigFile);

                            if (!silentFlag)
                            {
                                Console.WriteLine("Deleted " + n + " cache entries");
                                Console.WriteLine();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Failed clearing cache:");
                            printException(ex);
                            return 1;
                        }
                    }
                }

                if (path == null)
                {
                    // No path but cache was cleared or job file was initialized
                    return 0;
                }

                Regex? filePatternRegex = null;
                if (filePattern != null)
                {
                    try
                    {
                        filePatternRegex = new Regex(filePattern);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Invalid file pattern '" + filePattern + "': " + ex.Message);
                        Console.Error.WriteLine();
                        PrintUsage(true);
                        return 1;
                    }
                }

                List<string> files = new();

                Regex extensionPattern = new("\\.fit|\\.fits|\\.fts", RegexOptions.IgnoreCase);
                if (Directory.Exists(path))
                {
                    try
                    {
                        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                            .Where(file => extensionPattern.IsMatch(Path.GetExtension(file) ?? "") && (filePatternRegex == null || filePatternRegex.IsMatch(file))))
                        {
                            files.Add(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Could not read files in directory '" + path + "'");
                        printException(ex);
                        return 1;
                    }
                }
                else
                {
                    Console.Error.WriteLine("Could not find directory '" + path + "'");
                    return 1;
                }

                try
                {
                    if (!silentFlag)
                    {
                        Console.WriteLine("Evaluating " + files.Count + " files...");
                        Console.WriteLine();
                    }

                    var progressStatus = new ProgressStatus();

                    void finish(bool results, bool newline)
                    {
                        lock (progressStatus)
                        {
                            if (!progressStatus.done)
                            {
                                if (progressStatus.progressWritten) Console.WriteLine();
                                if (progressStatus.progressWritten && newline) Console.WriteLine();

                                if (results && !silentFlag && outputFlag)
                                {
                                    Console.WriteLine("Results:");
                                    Console.WriteLine();
                                }
                            }
                            progressStatus.done = true;
                        }
                    }

                    Task.Run(async () =>
                    {
                        using CancellationTokenSource progressTrackerCts = new();
                        CancellationToken progressTrackerCt = progressTrackerCts.Token;

                        BatchEvaluationProgressTracker? tracker = null;
                        Task? progressTrackerTask = null;
                        if (progressFlag)
                        {
                            tracker = BatchEvaluationProgressTracker.Track(files.Count, out progressTrackerTask, 1000, progressTrackerCt);

                            int prevCharCount = 0;
                            DateTime startTime = DateTime.Now;

                            tracker.ProgressChanged += (s, status) =>
                            {
                                StringBuilder sb = new();

                                // Clear previous message
                                for (int i = 0; i < prevCharCount; ++i)
                                {
                                    sb.Append('\b');
                                }
                                for (int i = 0; i < prevCharCount; ++i)
                                {
                                    sb.Append(' ');
                                }
                                for (int i = 0; i < prevCharCount; ++i)
                                {
                                    sb.Append('\b');
                                }

                                int backspaceCount = sb.Length;

                                lock (progressStatus)
                                {
                                    if (status.FilesLoaded < status.FilesTotal)
                                    {
                                        sb.Append("Loading progress: ");
                                        sb.Append(status.PhaseProgress);
                                        sb.Append('/');
                                        sb.Append(status.FilesTotal);
                                        sb.Append(" files   ");
                                        sb.Append(string.Format("{0:0.##} files/s", status.PhaseSpeedEstimate));
                                    }
                                    else
                                    {
                                        sb.Append("Evaluation progress: ");
                                        sb.Append(status.PhaseProgress);
                                        sb.Append(" %   ");
                                        sb.Append(string.Format("{0:0.##} %/s", status.PhaseSpeedEstimate));
                                    }

                                    var timespan = (DateTime.Now - startTime);
                                    sb.Append("   ");
                                    sb.Append(timespan.Days > 0 ? string.Format("{0:dd\\:hh\\:mm\\:ss}", timespan) : string.Format("{0:hh\\:mm\\:ss}", timespan));
                                }

                                prevCharCount = sb.Length - backspaceCount;

                                if (sb.Length > 0)
                                {
                                    lock (progressStatus)
                                    {
                                        if (progressStatus.done)
                                        {
                                            progressTrackerCts.Cancel();
                                        }

                                        Console.Write(sb);

                                        progressStatus.progressWritten = true;
                                    }
                                }
                            };
                        }

                        void onExporterConfirmation(object? sender, ConfirmationEventArgs e)
                        {
                            if (!yesFlag)
                            {
                                e.RegisterHandler(ct =>
                                {
                                    while (true)
                                    {
                                        Console.WriteLine();
                                        Console.WriteLine($"The '{e.RequesterName}' exporter requires confirmation: ");
                                        Console.WriteLine(e.Message);
                                        Console.WriteLine("If aborted, this exporter will be skipped. Proceed? [y/n] ");
                                        var input = Console.ReadKey(false).Key;
                                        if (input != ConsoleKey.Enter) Console.WriteLine();
                                        if (input == ConsoleKey.Y)
                                        {
                                            return Task.FromResult(ConfirmationEventArgs.Result.Proceed);
                                        }
                                        else if (input == ConsoleKey.N)
                                        {
                                            return Task.FromResult(ConfirmationEventArgs.Result.Abort);
                                        }
                                    }
                                });
                            }
                        }

                        evaluator.OnExporterConfirmation += onExporterConfirmation;

                        var evaluateTask = evaluator.EvaluateAsync(jobConfigFile, files, (file, groupKey, variableValues, value, ct) =>
                        {
                            if (outputFlag)
                            {
                                lock (progressStatus)
                                {
                                    finish(true, true);

                                    Console.WriteLine("File: " + file);
                                    Console.WriteLine("Value: " + value);
                                    Console.WriteLine("GroupKey: " + groupKey);
                                    Console.WriteLine("Variables:");
                                    foreach (var entry in variableValues)
                                    {
                                        Console.WriteLine("  " + entry.Key + " " + entry.Value);
                                    }

                                    Console.WriteLine();
                                }
                            }

                            return Task.CompletedTask;
                        }, (tracker != null || debugFlag) ? e =>
                        {
                            lock (progressStatus)
                            {
                                tracker?.OnEvent(e);

                                if (debugFlag && e.Error != null)
                                {
                                    Console.WriteLine();
                                    if (e is BatchEvaluation.FileEvent fe)
                                    {
                                        Console.Error.WriteLine("An error occurred during the evaluation of file '" + fe.File + "':");
                                    }
                                    printException(e.Error);
                                    Console.WriteLine();
                                }
                            }
                        }
                        : null);

                        try
                        {
                            await evaluateTask;
                        }
                        catch (Exception)
                        {
                            lock (progressStatus)
                            {
                                finish(false, true);
                            }

                            throw;
                        }
                        finally
                        {
                            evaluator.OnExporterConfirmation -= onExporterConfirmation;

                            progressTrackerCts.Cancel();
                        }

                        if (progressTrackerTask != null)
                        {
                            await progressTrackerTask;
                        }

                    }).Wait();

                    if (!silentFlag)
                    {
                        lock (progressStatus)
                        {
                            finish(false, true);

                            Console.WriteLine("Done");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Evaluation failed due to an unhandled error:");
                    printException(ex);
                    return 1;
                }

                return 0;
            }
            else
            {
                if (jobConfigFile == null)
                {
                    Console.Error.WriteLine("No job config file specified");
                }
                else if (path == null)
                {
                    Console.Error.WriteLine("No FITs directory specified");
                }

                Console.Error.WriteLine();
                PrintUsage(true);

                return 1;
            }
        }

        private static void PrintUsage(bool err)
        {
            var writer = err ? Console.Error : Console.Out;

            string file = "" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            writer.WriteLine("Usage:");
            writer.WriteLine("  " + file + " [options]");
            writer.WriteLine();
            writer.WriteLine("Options:");
            writer.WriteLine("  -c,\t--clearcache\t\tclear the cache");
            writer.WriteLine("  -d,\t--debug\t\t\tdisplay debug messages");
            writer.WriteLine("  -e,\t--exporters\t\tdisplay list of available exporters");
            writer.WriteLine("  -f,\t--filepattern <pattern>\tregex file pattern for filtering FITS files");
            writer.WriteLine("  -h,\t--help\t\t\tdisplay this help message");
            writer.WriteLine("  -i,\t--init\t\t\tcreate a new job config file if it doesn't already exist");
            writer.WriteLine("  -j,\t--job <path>\t\tpath of the job config file");
            writer.WriteLine("  -o,\t--output\t\twrite all results to standard output");
            writer.WriteLine("  -p,\t--path <path>\t\tpath of the directory containing FITS files");
            writer.WriteLine("  \t--progress\t\tdisplay progress messages");
            writer.WriteLine("  -s,\t--silent\t\tsuppress all standard output except for progress messages and results");
            writer.WriteLine("  -y,\t--yes\t\t\tsuppress confirmations for --clearcache or potentially dangerous exporters");
            writer.WriteLine();
            writer.WriteLine("Use the following command to get started quickly:");
            writer.WriteLine("  " + file + " --job job.json --init --path <path_to_fits_files> --output --progress");
        }
    }
}
