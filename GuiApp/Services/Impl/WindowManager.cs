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

using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class WindowManager : IWindowManager
    {
        private readonly Dictionary<Type, List<Window>> windows = new();

        private readonly Dictionary<Window, WindowState> restoreStates = new();

        public IEnumerable<Window> Windows => windows.SelectMany(pair => pair.Value);

        public bool Show<T>(Func<T> factory, bool showMultiple, Func<T, bool>? filter = null) where T : Window
        {
            var type = typeof(T);

            windows.TryGetValue(type, out var list);

            T? window = null;

            if (showMultiple || list == null || list.Count == 0)
            {
                window = factory();
            }
            else if (list != null && filter != null)
            {
                bool hasMatchingWindow = false;

                foreach (var w in list)
                {
                    if (filter((T)w))
                    {
                        hasMatchingWindow = true;
                        break;
                    }
                }

                if (!hasMatchingWindow)
                {
                    window = factory();
                }
            }

            if (window != null)
            {
                windows[type] = list ??= new();
                list.Add(window);

                var stateObservableSubscription = window.GetObservable(Window.WindowStateProperty).Subscribe(state =>
                {
                    if (state != WindowState.Minimized)
                    {
                        restoreStates.Remove(window);
                    }
                });

                void onClosed(object? sender, EventArgs e)
                {
                    window.Closed -= onClosed;

                    stateObservableSubscription.Dispose();

                    if (windows.TryGetValue(type, out var currentList) && currentList != null)
                    {
                        currentList.Remove(window);
                        if (currentList.Count == 0)
                        {
                            windows.Remove(type);
                        }
                    }

                    _windowClosed?.Invoke(this, new IWindowManager.WindowEventArgs(window));
                }
                window.Closed += onClosed;

                window.Show();

                _windowOpened?.Invoke(this, new IWindowManager.WindowEventArgs(window));

                return true;
            }

            if (list != null && list.Count > 0)
            {
                if (list[0].WindowState == WindowState.Minimized)
                {
                    list[0].WindowState = WindowState.Normal;
                }
                list[0].Activate();
            }

            return false;
        }

        public IEnumerable<T> Get<T>() where T : Window
        {
            if (windows.TryGetValue(typeof(T), out var list))
            {
                return list.Cast<T>();
            }
            return new List<T>();
        }

        public void MinimizeAll()
        {
            foreach (var window in Windows)
            {
                restoreStates.TryAdd(window, window.WindowState);
                window.WindowState = WindowState.Minimized;
            }
        }

        public void RestoreAll()
        {
            foreach (var window in restoreStates.Keys)
            {
                if (restoreStates.TryGetValue(window, out var restoreState))
                {
                    window.WindowState = restoreState;
                }
            }
            restoreStates.Clear();
        }

        private EventHandler<IWindowManager.WindowEventArgs>? _windowOpened;
        public event EventHandler<IWindowManager.WindowEventArgs> WindowOpened
        {
            add => _windowOpened += value;
            remove => _windowOpened -= value;
        }

        private EventHandler<IWindowManager.WindowEventArgs>? _windowClosed;
        public event EventHandler<IWindowManager.WindowEventArgs> WindowClosed
        {
            add => _windowClosed += value;
            remove => _windowClosed -= value;
        }
    }
}
