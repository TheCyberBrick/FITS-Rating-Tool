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

namespace VoyagerAPI.Runner
{
    public class SilentRunnerLog : IRunnerLog
    {
        public bool NotifySupported => false;

        private EventHandler<LogEvent>? _onLog;
        public event EventHandler<LogEvent> OnLog
        {
            add => _onLog += value;
            remove => _onLog -= value;
        }

        public SilentRunnerLog() { }

        public void Debug(string text) { }

        public void Error(string text) { }

        public void Info(string text) { }

        public void Notify(Level level, string title, string text, Action<INotificationInitializer>? initializer = null) { }

        public void Warning(string text) { }

        public void Log(Level level, string text) { }
    }
}
