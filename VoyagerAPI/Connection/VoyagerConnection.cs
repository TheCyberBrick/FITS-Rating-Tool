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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace VoyagerAPI.Connection
{
    public class VoyagerConnection : IDisposable, IVoyagerCommandSender, IVoyagerEventReceiver
    {
        private readonly string? hostname;
        private readonly int? port;

        private TcpClient? client;
        private Stream? stream;

        private TextWriter writer = null!;
        private JsonRpc rpc = null!;
        private VoyagerMessageHandler handler = null!;

        public bool Authenticated
        {
            get;
            private set;
        } = false;

        public bool Connected
        {
            get;
            private set;
        } = false;

        public event EventHandler<VoyagerEvent> OnEvent
        {
            add
            {
                handler.OnEvent += value;
            }
            remove
            {
                handler.OnEvent -= value;
            }
        }

        private DateTime heartbeatTime;
        private Task? heartbeatTask;
        private readonly CancellationTokenSource heartbeatCts = new();

        private Exception? exception;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> commands = new();

        public VoyagerConnection(Stream stream)
        {
            client = null;
            this.stream = stream;
        }

        public VoyagerConnection(string hostname, int port)
        {
            this.hostname = hostname;
            this.port = port;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (stream == null)
            {
                if (hostname == null || !port.HasValue)
                {
                    throw new InvalidOperationException("No stream or hostname and port specified");
                }
                client = new TcpClient();
                await client.ConnectAsync(hostname, port.Value, cancellationToken);
                stream = client.GetStream();
            }
            Init(stream);
        }

        private void Init(Stream stream)
        {
            if (stream == null)
            {
                throw new VoyagerNotConnectedException();
            }

            writer = TextWriter.Synchronized(new StreamWriter(stream, System.Text.Encoding.ASCII));

            handler = new VoyagerMessageHandler(stream, writer, stream, System.Text.Encoding.ASCII);

            heartbeatTime = DateTime.Now;
            handler.OnMessage += (_, e) => heartbeatTime = DateTime.Now;
            handler.OnEvent += (_, evt) => HandleOnEvent(evt);

            rpc = new JsonRpc(handler)
            {
                JsonSerializerFormatting = Formatting.None
            };
            rpc.StartListening();

            Connected = true;

            heartbeatTask = HeartbeatAsync();
        }

        private async Task HeartbeatAsync()
        {
            var token = heartbeatCts.Token;

            while (Connected && !token.IsCancellationRequested)
            {
                var delayTask = Task.Delay(TimeSpan.FromSeconds(5), token);

                try
                {
                    await SendJsonAsync(new JObject(
                        new JProperty("Event", "Polling"),
                        new JProperty("Timestamp", DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds),
                        new JProperty("Host", "TestHost"),
                        new JProperty("Inst", 1)
                        ), token);
                }
                catch (Exception ex)
                {
                    await CloseAsync();
                    throw exception = ex;
                }

                if ((DateTime.Now - heartbeatTime).TotalSeconds > 15)
                {
                    await CloseAsync();
                    throw exception = new VoyagerTimeoutException();
                }

                await delayTask;
            }
        }

        private void CheckException(bool requiresConnection = true)
        {
            if (exception != null)
            {
                throw exception;
            }
            if (requiresConnection && !Connected)
            {
                throw exception = new VoyagerNotConnectedException();
            }
        }

        private void HandleOnEvent(VoyagerEvent evt)
        {
            if (evt.Name.Equals("RemoteActionResult") && evt.Args.TryGetValue("UID", out JToken? uidToken))
            {
                string cmdGuid = uidToken.ToString();

                if (commands.TryRemove(cmdGuid, out TaskCompletionSource<JObject>? tcs))
                {
                    tcs.TrySetResult(evt.Args);
                }
            }
        }

        public async Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!Connected)
            {
                throw new VoyagerNotConnectedException();
            }

            CheckException();

            Authenticated = false;

            await rpc.InvokeWithParameterObjectAsync("AuthenticateUserBase", new
            {
                UID = Guid.NewGuid().ToString(),
                Base = System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(username + ":" + password))
            }, cancellationToken);

            Authenticated = true;
        }

        public async Task<VoyagerCommandResult> SendCommandAsync(string name, JObject args, bool hasResult = true, Action<string, JObject>? messageProcessor = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!Connected)
            {
                throw new VoyagerNotConnectedException();
            }

            CheckException();

            // Create a unique ID for command
            string cmdGuid = Guid.NewGuid().ToString();

            // Set args
            messageProcessor?.Invoke(cmdGuid, args);

            // Make sure args contains UID
            args.Remove("UID");
            args.Add("UID", cmdGuid);

            // Send command via JSON-RPC to Voyager
            await rpc.InvokeWithParameterObjectAsync(name, args, cancellationToken);

            if (hasResult)
            {
                // Wait for event response
                var tcs = new TaskCompletionSource<JObject>();
                using (cancellationToken.Register(() => tcs.SetCanceled()))
                {
                    commands.TryAdd(cmdGuid, tcs);
                    JObject eventObj = await tcs.Task;

                    // Return command result
                    return new VoyagerCommandResult(cmdGuid,
                            eventObj.GetValue("ActionResultInt")!.ToObject<int>(),
                            eventObj.TryGetValue("Motivo", out var motivoToken) && motivoToken.ToString().Length > 0 ? motivoToken.ToString() : null,
                            eventObj.GetValue("ParamRet")!.ToObject<JObject>()!
                            );
                }
            }
            else
            {
                return new VoyagerCommandResult(cmdGuid, 0, null, new());
            }
        }

        public async Task<VoyagerCommandResult> SendCommandAsync(string name, JObject args, bool hasResult = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await SendCommandAsync(name, args, hasResult, null, cancellationToken);
        }

        public async Task SendJsonAsync(JObject obj, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckException();

            await writer.WriteLineAsync(obj.ToString(Formatting.None).ToCharArray(), cancellationToken);
            await writer.FlushAsync();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD003")]
        public async Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                heartbeatCts.Cancel();

                if (Connected)
                {
                    await SendCommandAsync("disconnect", new JObject(), false, null, cancellationToken);
                }

                if (heartbeatTask != null)
                {
                    await heartbeatTask;
                }
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            Connected = false;
            try
            {
                rpc?.Dispose();
            }
            finally
            {
                try
                {
                    writer?.Close();
                }
                finally
                {
                    try
                    {
                        stream?.Close();
                    }
                    finally
                    {
                        try
                        {
                            client?.Close();
                        }
                        finally
                        {
                            heartbeatCts.Dispose();
                        }
                    }
                }
            }
        }
    }
}
