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

using System.Collections.Concurrent;

namespace FitsRatingTool.Common.Models.Evaluation
{
    public class ConfirmationEventArgs : EventArgs
    {
        public enum Result
        {
            Proceed, Abort
        }

        private List<Func<CancellationToken, Task<Result>>> handlers = new();
        private ConcurrentDictionary<Func<CancellationToken, Task<Result>>, Task<Result>> tasks = new();

        public string RequesterName { get; }

        public object? Requester { get; }

        public string Message { get; }

        public ConfirmationEventArgs(string requesterName, object requester, string message)
        {
            RequesterName = requesterName;
            Requester = requester;
            Message = message;
        }

        public void RegisterHandler(Func<CancellationToken, Task<Result>> handler)
        {
            handlers.Add(handler);
        }

        public async Task<Result> HandleAsync(CancellationToken cancellationToken = default)
        {
            Result result = Result.Proceed;
            for (int i = 0; i < handlers.Count; ++i)
            {
                if (await tasks.GetOrAdd(handlers[i], handler => handler(cancellationToken)) != Result.Proceed)
                {
                    result = Result.Abort;
                }
            }
            return result;
        }
    }
}
