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

using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VoyagerAPI.Connection;
using VoyagerAPI.Runner;
using VoyagerAPI.Scheduler;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class VoyagerIntegration : IVoyagerIntegration
    {
        private class Module : IRunnerModule
        {
            public string Name => "Integration";

            private readonly struct FitInfo
            {
                public readonly string fileName;
                public readonly string sequenceTarget;
                public readonly string? imageData;

                public FitInfo(string fileName, string sequenceTarget, string? imageData)
                {
                    this.fileName = fileName;
                    this.sequenceTarget = sequenceTarget;
                    this.imageData = imageData;
                }
            }

            private readonly ConcurrentQueue<FitInfo> newFitQueue = new();
            private TaskProvider.ISignal? newFitSignal;


            private IVoyagerCommandSenderWithMAC? connection;

            private CancellationTokenSource? cts;
            private readonly VoyagerIntegration integration;

            public Module(VoyagerIntegration integration)
            {
                this.integration = integration;
            }

            public void Init(IVoyagerCommandSenderWithMAC connection, IRunnerLog log)
            {
                this.connection = connection;
                this.connection.OnEvent += OnEvent;
            }

            private void OnEvent(object? sender, VoyagerEvent e)
            {
                if ("NewJPGReady".Equals(e.Name))
                {
                    newFitQueue.Enqueue(new FitInfo
                    (
                        Path.GetFullPath(e.Args.GetValue("File")!.ToString()),
                        e.Args.GetValue("SequenceTarget")!.ToString(),
                        e.Args.GetValue("Base64Data")!.ToString()
                    ));
                    newFitSignal?.Notify();
                }
                else if ("NewFITReady".Equals(e.Name))
                {
                    newFitQueue.Enqueue(new FitInfo
                    (
                        Path.GetFullPath(e.Args.GetValue("File")!.ToString()),
                        e.Args.GetValue("SeqTarget")!.ToString(),
                        null
                    ));
                    newFitSignal?.Notify();
                }
            }

            public void OnLoaded()
            {
            }

            public async Task RunAsync()
            {
                if (cts != null)
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch (Exception)
                    {
                    }
                }

                using (cts = new CancellationTokenSource())
                {
                    var ct = cts.Token;

                    SerialTaskScheduler scheduler = new();

                    scheduler.Schedule(TaskProvider.Create(HandleNewFitAsync, out newFitSignal), ct);

                    try
                    {
                        while (!ct.IsCancellationRequested && !scheduler.Empty)
                        {
                            await scheduler.ProcessAsync(ct);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // OK
                    }
                }
            }

            private Task HandleNewFitAsync()
            {
                while (newFitQueue.TryDequeue(out var fit))
                {
                    integration._newImage?.Invoke(this, new IVoyagerIntegration.NewImageEventArgs(fit.fileName));
                }
                return Task.CompletedTask;
            }

            public List<Tuple<string, Action>> Settings()
            {
                return new();
            }

            public void Stop()
            {
                if (connection != null)
                {
                    connection.OnEvent -= OnEvent;
                }
                try
                {
                    cts?.Cancel();
                }
                catch (Exception)
                {
                }
                cts?.Dispose();
            }
        }

        public string ApplicationServerHostname { get; set; } = "localhost";

        public int ApplicationServerPort { get; set; } = 5950;

        public string? ApplicationServerUsername { get; set; }

        public string? ApplicationServerPassword { get; set; }

        public string? RoboTargetSecret { get; set; }


        private bool connected = false;

        private Runner? runner;
        private Task? runnerTask;

        public Task StartAsync()
        {
            if (runner == null && runnerTask == null)
            {
                runner = new Runner(() => ApplicationServerHostname, () => ApplicationServerPort,
                    !string.IsNullOrEmpty(ApplicationServerUsername) || !string.IsNullOrEmpty(ApplicationServerPassword) ? new Runner.Credentials() { Username = ApplicationServerUsername ?? "", Password = ApplicationServerPassword ?? "" } : null,
                    !string.IsNullOrEmpty(RoboTargetSecret) ? new Runner.Credentials() { Password = RoboTargetSecret } : null,
                    m => new SilentRunnerLog());

                runner.OnStateChange += OnStateChanged;

                runner.AddModule(new Module(this));

                runnerTask = RunAsync(runner);
            }

            return Task.CompletedTask;
        }

        private static async Task RunAsync(Runner runner)
        {
            try
            {
                while (!runner.IsShutdown)
                {
                    try
                    {
                        await runner.RunAsync();
                    }
                    catch (Exception ex)
                    {
                        if (ex is AggregateException aex)
                        {
                            aex.Flatten().Handle(e => e is SocketException);
                        }
                        else if (ex is not SocketException)
                        {
                            throw;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Runner was stopped
            }
        }

        public async Task StopAsync()
        {
            if (runner != null)
            {
                await runner.ShutdownAsync();
                if (runnerTask != null) await runnerTask;
                runner.OnStateChange -= OnStateChanged;
                runner = null;
                runnerTask = null;
            }
        }

        private void OnStateChanged(object? sender, Runner.RunnerState state)
        {
            bool newConnectedState = false;

            switch (state)
            {
                case Runner.RunnerState.Connected:
                case Runner.RunnerState.Authenticated:
                case Runner.RunnerState.Running:
                    newConnectedState = true;
                    break;
            }

            if (connected != newConnectedState)
            {
                connected = newConnectedState;
                _connectionChanged?.Invoke(this, new IVoyagerIntegration.ConnectionChangedEventArgs(connected));
            }
        }

        private EventHandler<IVoyagerIntegration.ConnectionChangedEventArgs>? _connectionChanged;
        public event EventHandler<IVoyagerIntegration.ConnectionChangedEventArgs> ConnectionChanged
        {
            add
            {
                _connectionChanged += value;
            }
            remove
            {
                _connectionChanged -= value;
            }
        }

        private EventHandler<IVoyagerIntegration.NewImageEventArgs>? _newImage;
        public event EventHandler<IVoyagerIntegration.NewImageEventArgs> NewImage
        {
            add
            {
                _newImage += value;
            }
            remove
            {
                _newImage -= value;
            }
        }
    }
}
