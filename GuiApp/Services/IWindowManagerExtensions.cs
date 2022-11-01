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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Services
{
    public static class IWindowManagerExtensions
    {
        public static bool Show<TWindow, TData, TTemplate>(this IWindowManager manager, ITemplatedFactory<TData, TTemplate> factory, bool showMultiple, [NotNullWhen(true)] out TWindow? window, Func<TWindow, bool>? filter = null, Window? parent = null)
            where TWindow : Window
            where TData : class
        {
            return manager.Show<TWindow, TData, TTemplate>(container => factory.Instantiate(container.Instantiate), showMultiple, out window, filter, parent);
        }

        public static bool Show<TWindow, TData, TTemplate>(this IWindowManager manager, TTemplate template, bool showMultiple, [NotNullWhen(true)] out TWindow? window, Func<TWindow, bool>? filter = null, Window? parent = null)
            where TWindow : Window
            where TData : class
        {
            return manager.Show<TWindow, TData, TTemplate>(container => container.Instantiate(template), showMultiple, out window, filter, parent);
        }

        public static bool ShowDialog<TWindow, TData, TTemplate>(this IWindowManager manager, ITemplatedFactory<TData, TTemplate> factory, bool showMultiple, Window parent, [NotNullWhen(true)] out TWindow? window, [NotNullWhen(true)] out Task? task, Func<TWindow, bool>? filter = null)
            where TWindow : Window
            where TData : class
        {
            return manager.ShowDialog<TWindow, TData, TTemplate>(container => factory.Instantiate(container.Instantiate), showMultiple, parent, out window, out task, filter);
        }

        public static bool ShowDialog<TWindow, TData, TTemplate>(this IWindowManager manager, TTemplate template, bool showMultiple, Window parent, [NotNullWhen(true)] out TWindow? window, [NotNullWhen(true)] out Task? task, Func<TWindow, bool>? filter = null)
            where TWindow : Window
            where TData : class
        {
            return manager.ShowDialog<TWindow, TData, TTemplate>(container => container.Instantiate(template), showMultiple, parent, out window, out task, filter);
        }

        public static bool ShowDialog<TWindow, TData, TTemplate, R>(this IWindowManager manager, ITemplatedFactory<TData, TTemplate> factory, bool showMultiple, Window parent, [NotNullWhen(true)] out TWindow? window, [NotNullWhen(true)] out Task<R>? task, Func<TWindow, bool>? filter = null)
            where TWindow : Window
            where TData : class
        {
            return manager.ShowDialog<TWindow, TData, TTemplate, R>(container => factory.Instantiate(container.Instantiate), showMultiple, parent, out window, out task, filter);
        }

        public static bool ShowDialog<TWindow, TData, TTemplate, R>(this IWindowManager manager, TTemplate template, bool showMultiple, Window parent, [NotNullWhen(true)] out TWindow? window, [NotNullWhen(true)] out Task<R>? task, Func<TWindow, bool>? filter = null)
            where TWindow : Window
            where TData : class
        {
            return manager.ShowDialog<TWindow, TData, TTemplate, R>(container => container.Instantiate(template), showMultiple, parent, out window, out task, filter);
        }
    }
}
