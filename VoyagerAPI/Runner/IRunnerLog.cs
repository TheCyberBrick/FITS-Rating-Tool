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

using System;

namespace VoyagerAPI.Runner
{
    public enum Level
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public struct LogEvent
    {
        public Level Level;
        public string Text;
        public DateTime Time;
    }

    public interface INotificationInitializer
    {
    }

    public interface IRunnerLog
    {
        event EventHandler<LogEvent> OnLog;
        void Log(Level level, string text);
        void Info(string text);
        void Warning(string text);
        void Error(string text);
        void Debug(string text);

        bool NotifySupported { get; }
        void Notify(Level level, string title, string text, Action<INotificationInitializer>? initializer = null);
    }
}
