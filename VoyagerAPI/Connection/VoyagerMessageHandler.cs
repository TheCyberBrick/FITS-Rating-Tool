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
using StreamJsonRpc;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VoyagerAPI.Connection
{
    public class VoyagerMessageHandler : DelimitedMessageHandler
    {
        private StreamReader reader;
        private TextWriter writer;

        public event EventHandler? OnMessage;
        public event EventHandler<VoyagerEvent>? OnEvent;

        public VoyagerMessageHandler(Stream sendingStream, TextWriter writer, Stream receivingStream, Encoding encoding) : base(sendingStream, receivingStream, encoding)
        {
            reader = new StreamReader(receivingStream, encoding);
            this.writer = writer;
        }

        protected override async Task<string> ReadCoreAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();

                if (line == null || line.Length == 0)
                {
                    continue;
                }

                JObject obj = JObject.Parse(line);

                OnMessage?.Invoke(this, EventArgs.Empty);

                if (obj["jsonrpc"] == null && obj.TryGetValue("Event", out var eventToken))
                {
                    // Received event
                    OnEvent?.Invoke(this, new VoyagerEvent(eventToken.ToString(), obj));
                    continue;
                }
                else if (obj.TryGetValue("authbase", out var authbaseToken))
                {
                    // Special case: Voyager's response to AuthenticateUserBase does not
                    // have a JSON-RPC 2.0 response field. Thus, the "authbase" value is
                    // moved to "result" so it can be parsed by StreamJsonRpc.
                    obj.Remove("authbase");
                    obj.Add(new JProperty("result", authbaseToken));
                    line = obj.ToString(Newtonsoft.Json.Formatting.None);
                }

                // Let StreamJsonRpc handle JSON-RPC messages or throw
                // exceptions if invalid message
                return line;
            }

            return null!;
        }

        protected override async Task WriteCoreAsync(string content, Encoding contentEncoding, CancellationToken cancellationToken)
        {
            JObject obj = JObject.Parse(content);
            obj.Remove("jsonrpc"); //Voyager doesn't seem like the jsonrpc field in requests

            if ("$/cancelRequest".Equals(obj.GetValue("method")?.ToString()))
            {
                reader.Close();
                writer.Close();
                throw new OperationCanceledException("JSON RPC cancellations are not supported");
            }

            await writer.WriteLineAsync(obj.ToString(Newtonsoft.Json.Formatting.None));
            await writer.FlushAsync();
        }
    }
}
