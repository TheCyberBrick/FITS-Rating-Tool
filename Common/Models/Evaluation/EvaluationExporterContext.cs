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

using static FitsRatingTool.Common.Models.Evaluation.IEvaluationExporterEventDispatcher;

namespace FitsRatingTool.Common.Models.Evaluation
{
    public abstract class EvaluationExporterContext : IEvaluationExporterContext, IDisposable
    {
        public abstract string ResolvePath(string path);

        private volatile bool cleanupDone = false;

        private EventHandler<ExporterEventArgs>? _onExporterEvent;
        public event EventHandler<ExporterEventArgs> OnExporterEvent
        {
            add => _onExporterEvent += value;
            remove => _onExporterEvent -= value;
        }

        private EventHandler? _onExporterCleanup;
        public event EventHandler OnExporterCleanup
        {
            add => _onExporterCleanup += value;
            remove => _onExporterCleanup -= value;
        }

        public void Send(IEvaluationExporter sender, string name, object? parameter)
        {
            _onExporterEvent?.Invoke(sender, new ExporterEventArgs(name, parameter));
        }

        public void Cleanup()
        {
            if (!cleanupDone)
            {
                cleanupDone = true;
                _onExporterCleanup?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
