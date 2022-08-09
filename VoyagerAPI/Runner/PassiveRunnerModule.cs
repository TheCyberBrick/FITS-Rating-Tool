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

using VoyagerAPI.Connection;

namespace VoyagerAPI.Runner
{
    public abstract class PassiveRunnerModule : IRunnerModule
    {
        public abstract string Name { get; }

        public IVoyagerCommandSenderWithMAC? Connection { get; private set; }

        private CancellationTokenSource? cts;

        public void Init(IVoyagerCommandSenderWithMAC connection, IRunnerLog log)
        {
            Connection = connection;
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

                try
                {
                    while (true)
                    {
                        await Task.Delay(1000, ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    // OK
                }
            }
        }

        public List<Tuple<string, Action>> Settings()
        {
            return new();
        }

        public void Stop()
        {
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
}
