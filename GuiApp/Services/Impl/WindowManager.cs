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
using DryIoc;
using DryIocAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    [Export(typeof(IWindowManager)), SingletonReuse]
    public class WindowManager : IWindowManager
    {
        private readonly Dictionary<Type, List<Window>> windows = new();

        private readonly Dictionary<Window, WindowState> restoreStates = new();

        public IEnumerable<Window> Windows => windows.SelectMany(pair => pair.Value);

        private readonly IResolverContext resolver;

        public WindowManager(IResolverContext resolver)
        {
            this.resolver = resolver;
        }

        public bool Show<TWindow, TData, TTemplate>(Func<IContainer<TData, TTemplate>, TData> factory, bool showMultiple, [NotNullWhen(true)] out TWindow? window, Func<TWindow, bool>? filter, Window? parent)
            where TWindow : Window
            where TData : class
        {
            if (ShowImpl(factory, showMultiple, filter, out window))
            {
                if (parent != null)
                {
                    window.Show(parent);
                }
                else
                {
                    window.Show();
                }
                return true;
            }
            return false;
        }

        public bool ShowDialog<TWindow, TData, TTemplate>(Func<IContainer<TData, TTemplate>, TData> factory, bool showMultiple, Window parent, [NotNullWhen(true)] out TWindow? window, [NotNullWhen(true)] out Task? task, Func<TWindow, bool>? filter = null)
            where TWindow : Window
            where TData : class
        {
            if (ShowImpl(factory, showMultiple, filter, out window))
            {
                task = window.ShowDialog(parent);
                return true;
            }
            task = null;
            return false;
        }

        public bool ShowDialog<TWindow, TData, TTemplate, R>(Func<IContainer<TData, TTemplate>, TData> factory, bool showMultiple, Window parent, [NotNullWhen(true)] out TWindow? window, [NotNullWhen(true)] out Task<R>? task, Func<TWindow, bool>? filter = null)
            where TWindow : Window
            where TData : class
        {
            if (ShowImpl(factory, showMultiple, filter, out window))
            {
                task = window.ShowDialog<R>(parent);
                return true;
            }
            task = null;
            return false;
        }

        private bool ShowImpl<TWindow, TData, TTemplate>(Func<IContainer<TData, TTemplate>, TData> factory, bool showMultiple, Func<TWindow, bool>? filter, [NotNullWhen(true)] out TWindow? outWindow)
            where TWindow : Window
            where TData : class
        {
            var type = typeof(TWindow);

            windows.TryGetValue(type, out var list);

            TWindow? window = null;

            if (showMultiple || list == null || list.Count == 0)
            {
                window = resolver.Resolve<TWindow>();
            }
            else if (list != null && filter != null)
            {
                bool hasMatchingWindow = false;

                foreach (var w in list)
                {
                    if (filter((TWindow)w))
                    {
                        hasMatchingWindow = true;
                        break;
                    }
                }

                if (!hasMatchingWindow)
                {
                    window = resolver.Resolve<TWindow>();
                }
            }

            if (window != null)
            {
                var containerRoot = resolver.Resolve<IContainerRoot<TData, TTemplate>>();

                var disposable = containerRoot.Initialize(out var container);

                window.DataContext = factory.Invoke(container);

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

                    disposable.Dispose();

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

                _windowOpened?.Invoke(this, new IWindowManager.WindowEventArgs(window));

                outWindow = window;

                return true;
            }

            if (list != null && list.Count > 0)
            {
                if (list[0].WindowState == WindowState.Minimized)
                {
                    list[0].WindowState = WindowState.Normal;
                }

                list[0].Activate();

                outWindow = (TWindow)list[0];
            }
            else
            {
                outWindow = null;
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
