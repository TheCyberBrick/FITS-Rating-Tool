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
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FitsRatingTool.GuiApp.UI.FitsImage.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Concurrency;
using System.Timers;

namespace FitsRatingTool.GuiApp.UI.FitsImage.Windows
{
    public partial class FitsImagePeekViewerWindow : ReactiveWindow<FitsImageViewerViewModel>
    {
        private Timer? timer;

        public DirectProperty<FitsImagePeekViewerWindow, float> MovementOpacityProperty = AvaloniaProperty.RegisterDirect<FitsImagePeekViewerWindow, float>(nameof(MovementOpacity), o => o.MovementOpacity, (o, v) => o.MovementOpacity = v);

        private DateTime movementOpacityChangedTime = DateTime.Now;

        private float _movementOpacity = 2.0f;
        public float MovementOpacity
        {
            get => _movementOpacity;
            set
            {
                SetAndRaise(MovementOpacityProperty, ref _movementOpacity, value);
                movementOpacityChangedTime = DateTime.Now;
            }
        }

        public FitsImagePeekViewerWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            Activated += (_, _) =>
            {
                timer = new Timer(10);
                timer.Elapsed += OnTimer;
                timer.Start();
            };

            Closed += (_, _) =>
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Elapsed -= OnTimer;
                    timer.Dispose();
                    timer = null;
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnTimer(object? sender, ElapsedEventArgs e)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                const int recoveryTime = 200;
                if ((DateTime.Now - movementOpacityChangedTime).Milliseconds > recoveryTime)
                {

                    MovementOpacity = Math.Min(MovementOpacity + 0.1f, 2.0f);
                    movementOpacityChangedTime = DateTime.Now - TimeSpan.FromMilliseconds(recoveryTime);
                }

                Opacity = Opacity + (Math.Min(Math.Max(MovementOpacity, 0.0f), 1.0f) - Opacity) * 0.25; // Binding MovementOpacity in xaml doesn't seem to work the second time the window is opened
            });
        }
    }
}
