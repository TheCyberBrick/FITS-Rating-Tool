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

using Avalonia.Controls;
using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IWindowManager
    {
        public class WindowEventArgs : EventArgs
        {
            public Window Window { get; }

            public WindowEventArgs(Window window)
            {
                Window = window;
            }
        }

        IEnumerable<Window> Windows { get; }

        bool Show<T>(Func<T> factory, bool showMultiple, Func<T, bool>? filter = null) where T : Window;

        IEnumerable<T> Get<T>() where T : Window;

        void MinimizeAll();

        void RestoreAll();

        event EventHandler<WindowEventArgs> WindowOpened;

        event EventHandler<WindowEventArgs> WindowClosed;
    }
}
