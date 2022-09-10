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
using System.Text;

namespace FitsRatingTool.Common.Services.Impl
{
    public partial class StandaloneEvaluationService : IStandaloneEvaluationService
    {
        private class Context : EvaluationExporterContext
        {
            private readonly string workingDir;

            public Context(string workingDir)
            {
                this.workingDir = workingDir;
            }

            public override string ResolvePath(string path)
            {
                path = Environment.ExpandEnvironmentVariables(path);
                return (Path.IsPathRooted(path) ? "" : workingDir) + path;
            }
        }

        private readonly Dictionary<string, IStandaloneEvaluationService.ExporterFactory> exporterFactories = new();

        public IReadOnlyCollection<string> Exporters => exporterFactories.Keys;


        private readonly IJobConfigFactory jobConfigFactory;
        private readonly IBatchEvaluationService batchEvaluationService;

        public StandaloneEvaluationService(IJobConfigFactory jobConfigFactory, IBatchEvaluationService batchEvaluationService)
        {
            this.jobConfigFactory = jobConfigFactory;
            this.batchEvaluationService = batchEvaluationService;
        }

        public bool RegisterExporter(string id, IStandaloneEvaluationService.ExporterFactory exporterFactory)
        {
            return exporterFactories.TryAdd(id, exporterFactory);
        }

        public bool UnregisterExporter(string id)
        {
            return exporterFactories.Remove(id);
        }

        private IEvaluationExporter? CreateExporter(IEvaluationExporterContext ctx, string id, string config)
        {
            if (exporterFactories.TryGetValue(id, out var exporter))
            {
                return exporter.Invoke(ctx, config);
            }
            return null;
        }

        private void LoadConfig(string jobConfigFile, out IJobConfig jobConfig, out string workingDir, out Cache? cache)
        {
            jobConfig = jobConfigFactory.Load(File.ReadAllText(jobConfigFile, Encoding.UTF8));

            try
            {
                workingDir = Directory.GetParent(jobConfigFile)!.FullName + Path.DirectorySeparatorChar;
            }
            catch (Exception ex)
            {
                throw new IStandaloneEvaluationService.InvalidConfigException("Unable to find working dir", ex);
            }

            if (jobConfig.CachePath != null)
            {
                jobConfig.CachePath = Environment.ExpandEnvironmentVariables(jobConfig.CachePath);
                string cachePath = (Path.IsPathRooted(jobConfig.CachePath) ? "" : workingDir) + jobConfig.CachePath;

                try
                {
                    Directory.CreateDirectory(cachePath);
                }
                catch (Exception ex)
                {
                    throw new IStandaloneEvaluationService.InvalidConfigException("Unable to create cache path '" + cachePath + "'", ex);
                }

                cache = new Cache(cachePath);
            }
            else
            {
                cache = null;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method")]
        private async Task<List<IEvaluationExporter>> LoadExportersAsync(IEvaluationExporterContext ctx, IReadOnlyJobConfig jobConfig, StreamWriter? logWriter, CancellationToken cancellationToken)
        {
            List<IEvaluationExporter> exporters = new();

            if (jobConfig.Exporters != null)
            {
                foreach (var exporterConfig in jobConfig.Exporters)
                {
                    if (logWriter != null)
                    {
                        StringBuilder sb = new();
                        TimestampLog(sb);
                        sb.Append("LoadExporterStart");
                        sb.Append(" Id='");
                        sb.Append(exporterConfig.Id);
                        sb.Append('\'');
                        lock (logWriter)
                        {
                            logWriter.WriteLine(sb);
                        }
                    }

                    IEvaluationExporter? exporter = null;

                    try
                    {
                        try
                        {
                            exporter = CreateExporter(ctx, exporterConfig.Id, exporterConfig.Config);
                        }
                        catch (Exception ex)
                        {
                            throw new IStandaloneEvaluationService.InvalidExporterException("Failed creating exporter '" + exporterConfig.Id + "'", ex);
                        }
                        if (exporter == null)
                        {
                            throw new IStandaloneEvaluationService.InvalidExporterException("Unknown exporter '" + exporterConfig.Id + "'", null);
                        }
                    }
                    catch (Exception e)
                    {
                        if (logWriter != null)
                        {
                            StringBuilder sb = new();
                            TimestampLog(sb);
                            sb.Append("LoadExporterEnd");
                            sb.Append(" Id='");
                            sb.Append(exporterConfig.Id);
                            sb.Append('\'');
                            sb.Append(" Error='");
                            sb.Append(e.Message);
                            sb.Append('\'');
                            lock (logWriter)
                            {
                                logWriter.WriteLine(sb);
                            }
                        }
                        throw;
                    }

                    bool success = true;

                    var confirmationMessage = exporter.ConfirmationMessage;
                    if (confirmationMessage != null)
                    {
                        var args = new ConfirmationEventArgs(exporterConfig.Id, exporter, confirmationMessage);

                        _onExporterConfirmation?.Invoke(this, args);

                        success = await args.HandleAsync(cancellationToken) == ConfirmationEventArgs.Result.Proceed;
                    }

                    if (success)
                    {
                        exporters.Add(exporter);
                    }
                    else if (logWriter != null)
                    {
                        StringBuilder sb = new();
                        TimestampLog(sb);
                        sb.Append("LoadExporterAbort");
                        sb.Append(" Id='");
                        sb.Append(exporterConfig.Id);
                        sb.Append('\'');
                        lock (logWriter)
                        {
                            logWriter.WriteLine(sb);
                        }
                    }

                    if (logWriter != null)
                    {
                        StringBuilder sb = new();
                        TimestampLog(sb);
                        sb.Append("LoadExporterEnd");
                        sb.Append(" Id='");
                        sb.Append(exporterConfig.Id);
                        sb.Append('\'');
                        lock (logWriter)
                        {
                            logWriter.WriteLine(sb);
                        }
                    }
                }
            }

            return exporters;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD103")]
        public async Task EvaluateAsync(string jobConfigFile, List<string> files, IStandaloneEvaluationService.EvaluationConsumer? evaluationConsumer, IStandaloneEvaluationService.EventConsumer? eventConsumer, Action<string?>? logFileConsumer, CancellationToken cancellationToken)
        {
            StreamWriter? logWriter = null;
            List<IEvaluationExporter> exporters = new();

            try
            {
                LoadConfig(jobConfigFile, out var jobConfig, out var workingDir, out var cache);

                IBatchEvaluationService.EventConsumer? eventLogger = null;

                if (jobConfig.OutputLogsPath != null)
                {
                    jobConfig.OutputLogsPath = Environment.ExpandEnvironmentVariables(jobConfig.OutputLogsPath);
                    var logPath = (Path.IsPathRooted(jobConfig.OutputLogsPath) ? "" : workingDir) + jobConfig.OutputLogsPath;

                    try
                    {
                        Directory.CreateDirectory(logPath);
                    }
                    catch (Exception ex)
                    {
                        throw new IStandaloneEvaluationService.InvalidConfigException("Unable to create output log path '" + logPath + "'", ex);
                    }

                    string logFile = logPath + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(jobConfigFile) + "_" + DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss") + ".log";

                    try
                    {
                        logWriter = new(logFile, true);
                    }
                    catch (Exception ex)
                    {
                        throw new IStandaloneEvaluationService.InvalidConfigException("Unable to create output log file '" + logFile + "'", ex);
                    }

                    logFileConsumer?.Invoke(logFile);

                    eventLogger = e =>
                    {
                        StringBuilder sb = new();
                        TimestampLog(sb);
                        WriteEventToLog(e, sb);
                        lock (logWriter)
                        {
                            logWriter.WriteLine(sb);
                        }
                    };
                }
                else
                {
                    logFileConsumer?.Invoke(null);
                }

                if (logWriter != null)
                {
                    StringBuilder sb = new();
                    TimestampLog(sb);
                    sb.AppendLine("ConfigStart");
                    foreach (var line in jobConfigFactory.Save(jobConfig).Split(Environment.NewLine))
                    {
                        TimestampLog(sb);
                        sb.AppendLine(line);
                    }
                    TimestampLog(sb);
                    sb.AppendLine("ConfigEnd");
                    lock (logWriter)
                    {
                        logWriter.Write(sb);
                    }
                }

                using var ctx = new Context(workingDir);

                exporters = await LoadExportersAsync(ctx, jobConfig, logWriter, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                Task evaluationTask = batchEvaluationService.EvaluateAsync(jobConfig, files, async (file, groupKey, variableValues, value, ct) =>
                {
                    if (logWriter != null)
                    {
                        StringBuilder sb = new();
                        TimestampLog(sb);
                        sb.Append("ExportEvaluation");
                        sb.Append(" File='");
                        sb.Append(file);
                        sb.Append("' Value='");
                        sb.Append(value);
                        sb.Append('\'');
                        lock (logWriter)
                        {
                            logWriter.WriteLine(sb);
                        }
                    }

                    List<Exception>? errors = null;

                    foreach (var exporter in exporters)
                    {
                        try
                        {
                            await exporter.ExportAsync(ctx, file, groupKey, variableValues, value, ct);
                        }
                        catch (Exception ex)
                        {
                            errors ??= new();
                            errors.Add(new Exception(exporter.GetType().FullName + " failed to export the evaluation", ex));
                        }
                    }

                    try
                    {
                        evaluationConsumer?.Invoke(file, groupKey, variableValues, value, ct);
                    }
                    catch (Exception ex)
                    {
                        errors ??= new();
                        errors.Add(ex);
                    }

                    if (errors != null)
                    {
                        if (errors.Count == 1)
                        {
                            throw errors[0];
                        }
                        else
                        {
                            throw new AggregateException("Multiple exporters failed to export the evaluation", errors);
                        }
                    }
                }, cache, (eventLogger != null || eventConsumer != null) ? e =>
                {
                    eventLogger?.Invoke(e);
                    eventConsumer?.Invoke(e);
                }
                : null, cancellationToken);

                using (CancellationTokenSource flushCts = new())
                {
                    CancellationToken flushCt = flushCts.Token;

                    async Task FlushAsync()
                    {
                        if (logWriter != null)
                        {
                            try
                            {
                                while (!flushCt.IsCancellationRequested)
                                {
                                    await Task.Delay(1000, flushCt);

                                    lock (logWriter)
                                    {
                                        logWriter.Flush();
                                    }
                                }
                            }
                            catch (OperationCanceledException)
                            {
                            }
                        }
                    }

                    Task flushTask = FlushAsync();

                    await evaluationTask;

                    flushCts.Cancel();

                    await flushTask;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    await FlushAndCloseAsync(logWriter, exporters);
                }
                catch (Exception ex2)
                {
                    throw new AggregateException(ex, ex2);
                }
                throw;
            }

            await FlushAndCloseAsync(logWriter, exporters);
        }

        private static async Task FlushAndCloseAsync(StreamWriter? logWriter, List<IEvaluationExporter> exporters)
        {
            try
            {
                FlushAndCloseLogWriter(logWriter);
            }
            catch (Exception ex)
            {
                try
                {
                    await FlushAndCloseExportersAsync(exporters);
                }
                catch (Exception ex2)
                {
                    throw new AggregateException(ex, ex2);
                }
                throw;
            }

            await FlushAndCloseExportersAsync(exporters);
        }

        private static void FlushAndCloseLogWriter(StreamWriter? logWriter)
        {
            if (logWriter != null)
            {
                lock (logWriter)
                {
                    try
                    {
                        logWriter.Flush();
                    }
                    finally
                    {
                        logWriter.Close();
                    }
                }
            }
        }

        private static async Task FlushAndCloseExportersAsync(List<IEvaluationExporter> exporters)
        {
            List<Exception>? errors = null;

            foreach (var exporter in exporters)
            {
                try
                {
                    try
                    {
                        await exporter.FlushAsync();
                    }
                    finally
                    {
                        exporter.Close();
                    }
                }
                catch (Exception ex)
                {
                    errors ??= new();
                    errors.Add(ex);
                }
            }

            if (errors != null)
            {
                if (errors.Count == 1)
                {
                    throw errors[0];
                }
                else
                {
                    throw new AggregateException(errors);
                }
            }
        }

        private static void TimestampLog(StringBuilder sb)
        {
            sb.Append('[');
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.Append("] ");
        }

        private static void WriteEventToLog(BatchEvaluation.Event e, StringBuilder sb)
        {
            if (e is BatchEvaluation.EvaluationStepEvent ese)
            {
                sb.Append(Enum.GetName(typeof(BatchEvaluation.Phase), e.Phase));
                sb.Append(" File='");
                sb.Append(ese.File);
                sb.Append("' GroupKey='");
                sb.Append(ese.GroupKey);
                sb.Append("' GroupIndex='");
                sb.Append(ese.GroupIndex);
                sb.Append("' GroupCount='");
                sb.Append(ese.GroupCount);
                sb.Append("' StepIndex='");
                sb.Append(ese.StepIndex);
                sb.Append("' StepCount='");
                sb.Append(ese.StepCount);
                sb.Append('\'');
            }
            else if (e is BatchEvaluation.EvaluationEvent ee)
            {
                sb.Append(Enum.GetName(typeof(BatchEvaluation.Phase), e.Phase));
                sb.Append(" File='");
                sb.Append(ee.File);
                sb.Append("' GroupKey='");
                sb.Append(ee.GroupKey);
                sb.Append("' GroupIndex='");
                sb.Append(ee.GroupIndex);
                sb.Append("' GroupCount='");
                sb.Append(ee.GroupCount);
                sb.Append('\'');
            }
            else if (e is BatchEvaluation.LoadEvent le)
            {
                sb.Append(Enum.GetName(typeof(BatchEvaluation.Phase), e.Phase));
                sb.Append(" File='");
                sb.Append(le.File);
                if (e.Phase == BatchEvaluation.Phase.LoadFitEnd)
                {
                    sb.Append("' Cached='");
                    sb.Append(le.Cached);
                    sb.Append("' Skipped='");
                    sb.Append(le.Skipped);
                }
                sb.Append('\'');
            }
            else if (e is BatchEvaluation.FileEvent fe)
            {
                sb.Append(Enum.GetName(typeof(BatchEvaluation.Phase), e.Phase));
                sb.Append(" File='");
                sb.Append(fe.File);
                sb.Append('\'');
            }
            else if (e is BatchEvaluation.StatisticsEvent se)
            {
                sb.Append(Enum.GetName(typeof(BatchEvaluation.Phase), e.Phase));
                sb.Append(" Statistics='");
                sb.Append(se.Name);
                sb.Append("' GroupKey='");
                sb.Append(se.GroupKey);
                sb.Append("' GroupIndex='");
                sb.Append(se.GroupIndex);
                sb.Append("' GroupCount='");
                sb.Append(se.GroupCount);
                sb.Append('\'');
            }
            else
            {
                sb.Append(Enum.GetName(typeof(BatchEvaluation.Phase), e.Phase)!);
            }

            if (e.Error != null)
            {
                sb.Append(" Error='");
                sb.Append(e.Error.Message);
                sb.Append('\'');
            }
        }

        public string? GetCachePath(string jobConfigFile)
        {
            LoadConfig(jobConfigFile, out var _, out var _, out var cache);
            if (cache != null)
            {
                return cache.CachePath;
            }
            return null;
        }

        public int DeleteCache(string jobConfigFile, IEnumerable<string> file)
        {
            LoadConfig(jobConfigFile, out var _, out var _, out var cache);
            if (cache != null)
            {
                return cache.Delete(file);
            }
            return 0;
        }

        public int ClearCache(string jobConfigFile)
        {
            LoadConfig(jobConfigFile, out var _, out var _, out var cache);
            if (cache != null)
            {
                return cache.Clear();
            }
            return 0;
        }

        private EventHandler<ConfirmationEventArgs>? _onExporterConfirmation;
        public event EventHandler<ConfirmationEventArgs> OnExporterConfirmation
        {
            add => _onExporterConfirmation += value;
            remove => _onExporterConfirmation -= value;
        }
    }
}
