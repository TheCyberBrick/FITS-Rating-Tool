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
using Avalonia.Controls.Metadata;
using Avalonia.LogicalTree;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;

namespace FitsRatingTool.GuiApp.UI.Controls
{

    [PseudoClasses(":minimized", ":normal", ":maximized", ":fullscreen", ":pinned", ":unpinned")]
    public class WindowControlButton : Button
    {
        public ReactiveCommand<Unit, Unit>? Close { get; }
        public ReactiveCommand<Unit, Unit>? Minimize { get; }
        public ReactiveCommand<Unit, Unit>? Maximize { get; }
        public ReactiveCommand<Unit, Unit>? Normalize { get; }
        public ReactiveCommand<Unit, Unit>? ToggleMaximize { get; }
        public ReactiveCommand<Unit, Unit>? Pin { get; }
        public ReactiveCommand<Unit, Unit>? Unpin { get; }
        public ReactiveCommand<Unit, Unit>? TogglePin { get; }

        private Window? window;

        private CompositeDisposable? disposables;

        public WindowControlButton() : base()
        {
            Close = ReactiveCommand.Create(() =>
            {
                window?.Close();
            });
            Minimize = ReactiveCommand.Create(() =>
            {
                if (window != null)
                {
                    window.WindowState = WindowState.Minimized;
                }
            });
            Maximize = ReactiveCommand.Create(() =>
            {
                if (window != null)
                {
                    window.WindowState = WindowState.Maximized;
                }
            });
            Normalize = ReactiveCommand.Create(() =>
            {
                if (window != null)
                {
                    window.WindowState = WindowState.Normal;
                }
            });
            ToggleMaximize = ReactiveCommand.Create(() =>
            {
                if (window != null)
                {
                    window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }
            });
            Pin = ReactiveCommand.Create(() =>
            {
                if (window != null)
                {
                    window.Topmost = true;
                }
            });
            Unpin = ReactiveCommand.Create(() =>
            {
                if (window != null)
                {
                    window.Topmost = false;
                }
            });
            TogglePin = ReactiveCommand.Create(() =>
            {
                if (window != null)
                {
                    window.Topmost = !window.Topmost;
                }
            });
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            window = e.Root as Window;
            if (window != null)
            {
                disposables = new CompositeDisposable
                {
                    window.GetObservable(Window.WindowStateProperty).Subscribe(state =>
                    {
                        PseudoClasses.Set(":minimized", state == WindowState.Minimized);
                        PseudoClasses.Set(":normal", state == WindowState.Normal);
                        PseudoClasses.Set(":maximized", state == WindowState.Maximized);
                        PseudoClasses.Set(":fullscreen", state == WindowState.FullScreen);
                    }),
                    window.GetObservable(WindowBase.TopmostProperty).Subscribe(topmost =>
                    {
                        PseudoClasses.Set(":pinned", topmost);
                        PseudoClasses.Set(":unpinned", !topmost);
                    })
                };
            }

            base.OnAttachedToLogicalTree(e);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            disposables?.Dispose();
            disposables = null;
        }
    }
}
