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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoyagerAPI.Connection;

namespace VoyagerAPI.Runner
{
    public class Runner : IDisposable
    {
        public class Credentials
        {
            public string Username = "";
            public string Password = "";
        }

        private class RunnerConnection : IVoyagerCommandSenderWithMAC
        {
            private readonly VoyagerConnection connection;
            private readonly Credentials? roboTargetCredentials;

            public RunnerConnection(VoyagerConnection connection, Credentials? roboTargetCredentials)
            {
                this.connection = connection;
                this.roboTargetCredentials = roboTargetCredentials;
            }

            public event EventHandler<VoyagerEvent> OnEvent
            {
                add
                {
                    connection.OnEvent += value;
                }
                remove
                {
                    connection.OnEvent -= value;
                }
            }

            public Task<VoyagerCommandResult> SendCommandAsync(string name, JObject args, bool hasResult = true, Action<string, JObject>? messageProcessor = null, CancellationToken cancellationToken = default(CancellationToken))
            {
                return connection.SendCommandAsync(name, args, hasResult, messageProcessor, cancellationToken);
            }

            public Task<VoyagerCommandResult> SendCommandAsync(string name, JObject args, bool hasResult = true, CancellationToken cancellationToken = default(CancellationToken))
            {
                return connection.SendCommandAsync(name, args, hasResult, null, cancellationToken);
            }

            private class CommandSender : IVoyagerCommandSender
            {
                private readonly VoyagerConnection connection;
                private readonly Action<string, JObject> messageProcessor;

                public CommandSender(VoyagerConnection connection, Action<string, JObject> messageProcessor)
                {
                    this.connection = connection;
                    this.messageProcessor = messageProcessor;
                }

                public Task<VoyagerCommandResult> SendCommandAsync(string name, JObject args, bool hasResult = true, Action<string, JObject>? messageProcessor = null, CancellationToken cancellationToken = default(CancellationToken))
                {
                    return connection.SendCommandAsync(name, args, hasResult, (cmdGuid, obj) =>
                    {
                        messageProcessor?.Invoke(cmdGuid, obj);
                        this.messageProcessor.Invoke(cmdGuid, obj);
                    }, cancellationToken);
                }

                public Task<VoyagerCommandResult> SendCommandAsync(string name, JObject args, bool hasResult = true, CancellationToken cancellationToken = default(CancellationToken))
                {
                    return connection.SendCommandAsync(name, args, hasResult, messageProcessor, cancellationToken);
                }
            }

            public IVoyagerCommandSender WithMAC(params string[] fields)
            {
                return new CommandSender(connection, VoyagerMessageProcessors.WithMAC(roboTargetCredentials != null ? roboTargetCredentials.Password : "", fields));
            }

            public IVoyagerCommandSender WithMAC(Func<string, JToken, string> formatter, params string[] fields)
            {
                return new CommandSender(connection, VoyagerMessageProcessors.WithMAC(roboTargetCredentials != null ? roboTargetCredentials.Password : "", formatter, fields));
            }
        }

        public enum RunnerState
        {
            Stopped,
            Connecting,
            Connected,
            Authenticating,
            Authenticated,
            Running,
            Stopping
        }

        private RunnerState _state = RunnerState.Stopped;
        public RunnerState State
        {
            get
            {
                return _state;
            }
            private set
            {
                _state = value;
                OnStateChange?.Invoke(this, value);
            }
        }

        public event EventHandler<RunnerState>? OnStateChange;

        private readonly Func<string> hostname;
        private readonly Func<int> port;
        private readonly Credentials? applicationServerCredentials, roboTargetCredentials;
        private readonly Func<IRunnerModule, IRunnerLog> logger;

        private List<IRunnerModule> runnerModules = new();

        private CancellationTokenSource shutdownCts = new();
        private TaskCompletionSource<bool> shutdownTcs = new();

        public VoyagerConnection? Connection
        {
            get;
            private set;
        }

        public bool IsShutdown
        {
            get;
            private set;
        }

        public Runner(Func<string> hostname, Func<int> port, Credentials? applicationServerCredentials, Credentials? roboTargetCredentials, Func<IRunnerModule, IRunnerLog> logger)
        {
            this.hostname = hostname;
            this.port = port;
            this.applicationServerCredentials = applicationServerCredentials;
            this.roboTargetCredentials = roboTargetCredentials;
            this.logger = logger;
        }

        public void AddModule(IRunnerModule module)
        {
            runnerModules.Add(module);
        }

        private async Task AwaitTasksAsync(List<Task> tasks, int index, CancellationToken cancellationToken)
        {
            if (index < tasks.Count)
            {
                try
                {
                    await tasks[index].WaitAsync(cancellationToken);
                }
                finally
                {
                    await AwaitTasksAsync(tasks, index + 1, cancellationToken);
                }
            }
        }

        public class AuthenticationTimeoutException : Exception
        {
            public AuthenticationTimeoutException(string message, Exception ex) : base(message, ex)
            {
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var shutdownToken = shutdownCts.Token;

                using (cancellationToken.Register(() =>
                 {
                     try
                     {
                         shutdownCts.Cancel();
                     }
                     catch (ObjectDisposedException)
                     {
                         // Already shut down
                     }
                 }))
                {
                    State = RunnerState.Stopped;

                    while (!shutdownCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        State = RunnerState.Connecting;

                        VoyagerConnection? connection = null;
                        try
                        {
                            Connection = connection = new VoyagerConnection(hostname.Invoke(), port.Invoke());
                            await connection.ConnectAsync(cancellationToken);
                            State = RunnerState.Connected;
                            try
                            {
                                if (applicationServerCredentials != null)
                                {
                                    State = RunnerState.Authenticating;

                                    using (CancellationTokenSource authTcs = new(10000))
                                    {
                                        using (cancellationToken.Register(() =>
                                             {
                                                 try
                                                 {
                                                     authTcs.Cancel();
                                                 }
                                                 catch (ObjectDisposedException)
                                                 {
                                                 }
                                             }))
                                        {
                                            try
                                            {
                                                await connection.AuthenticateAsync(applicationServerCredentials.Username, applicationServerCredentials.Password, authTcs.Token);
                                            }
                                            catch (OperationCanceledException ex)
                                            {
                                                if (!cancellationToken.IsCancellationRequested)
                                                {
                                                    throw new AuthenticationTimeoutException("Authentication timed out", ex);
                                                }
                                                throw;
                                            }
                                        }
                                    }

                                    State = RunnerState.Authenticated;
                                }

                                IVoyagerCommandSenderWithMAC runnerConnection = new RunnerConnection(connection, roboTargetCredentials);

                                List<Task> tasks = new();
                                try
                                {
                                    foreach (IRunnerModule module in runnerModules)
                                    {
                                        module.Init(runnerConnection, logger.Invoke(module));
                                        tasks.Add(module.RunAsync());
                                    }

                                    State = RunnerState.Running;

                                    while (!shutdownToken.IsCancellationRequested && connection.Connected)
                                    {
                                        try
                                        {
                                            await Task.Delay(500, shutdownToken);
                                        }
                                        catch (OperationCanceledException)
                                        {
                                            // Don't care
                                        }
                                    }
                                }
                                finally
                                {
                                    State = RunnerState.Stopping;

                                    List<Exception>? exceptions = null;
                                    foreach (IRunnerModule module in runnerModules)
                                    {
                                        try
                                        {
                                            module.Stop();
                                        }
                                        catch (Exception ex)
                                        {
                                            if (exceptions == null) exceptions = new List<Exception>();
                                            exceptions.Add(ex);
                                        }
                                    }

                                    try
                                    {
                                        if (exceptions != null)
                                        {
                                            if (exceptions.Count == 1)
                                            {
                                                throw exceptions[0];
                                            }
                                            else
                                            {
                                                throw new AggregateException(exceptions);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        await AwaitTasksAsync(tasks, 0, cancellationToken);
                                    }
                                }
                            }
                            finally
                            {
                                State = RunnerState.Stopped;

                                using (CancellationTokenSource closeCts = new(1000))
                                {
                                    using (cancellationToken.Register(() =>
                                     {
                                         try
                                         {
                                             closeCts.Cancel();
                                         }
                                         catch (ObjectDisposedException)
                                         {
                                             // Already shut down
                                         }
                                     }))
                                    {
                                        try
                                        {
                                            await connection.CloseAsync(closeCts.Token);
                                        }
                                        catch (ObjectDisposedException)
                                        {
                                            // Already disconnected
                                        }
                                        catch (OperationCanceledException)
                                        {
                                            // Don't care except when explicitly cancelled
                                            if (cancellationToken.IsCancellationRequested)
                                            {
                                                throw;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            connection?.Dispose();
                            Connection = null;
                        }

                        if (!shutdownToken.IsCancellationRequested)
                        {
                            try
                            {
                                await Task.Delay(1000, shutdownToken);
                            }
                            catch (OperationCanceledException)
                            {
                                // Don't care
                            }
                        }
                    }
                }
            }
            finally
            {
                State = RunnerState.Stopped;

                if (shutdownCts.IsCancellationRequested)
                {
                    shutdownTcs.TrySetResult(true);
                    IsShutdown = true;
                }
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                shutdownCts.Cancel();
                if (State != RunnerState.Stopped)
                {
                    await shutdownTcs.Task;
                }
            }
            finally
            {
                IsShutdown = true;
            }
        }

        public void Dispose()
        {
            try
            {
                shutdownCts.Dispose();
            }
            finally
            {
                IsShutdown = true;
            }
        }
    }
}
