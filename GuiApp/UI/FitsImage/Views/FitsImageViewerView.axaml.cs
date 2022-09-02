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
using Avalonia.Controls.PanAndZoom;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Utilities;
using FitsRatingTool.GuiApp.UI.FitsImage.ViewModels;
using Avalonia.LogicalTree;
using FitsRatingTool.GuiApp.UI.FitsImage.Windows;
using System;
using Avalonia.Input;
using System.Threading;
using System.Threading.Tasks;
using static FitsRatingTool.GuiApp.UI.FitsImage.IFitsImageSectionViewerViewModel;

namespace FitsRatingTool.GuiApp.UI.FitsImage.Views
{
    public partial class FitsImageViewerView : ReactiveUserControl<FitsImageViewerViewModel>
    {
        private Window? window;

        private ZoomBorder? zoomBorder;
        private ContentControl? imageView;

        private bool isPeekViewerOpen;

        private double prevZoomX = 0;
        private double prevZoomY = 0;
        private Rect? prevBounds;

        private bool? prevInterpolated;

        public FitsImageViewerView()
        {
            InitializeComponent();

            zoomBorder = this.Find<ZoomBorder>("ZoomBorder");
            imageView = this.Find<ContentControl>("ImageView");

            zoomBorder.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && !e.KeyModifiers.HasFlag(KeyModifiers.Control) && window != null && imageView != null && !isPeekViewerOpen && ViewModel != null && ViewModel.IsPeekViewerEnabled)
                {
                    CancellationTokenSource cts = new();

                    void cancel()
                    {
                        try
                        {
                            cts.Cancel();
                        }
                        catch (Exception)
                        {
                            // Already disposed or cancelled
                        }
                        cts.Dispose();
                    }

                    void onReleased(object? sender, EventArgs e)
                    {
                        zoomBorder.PointerReleased -= onReleased;
                        cancel();
                    }
                    zoomBorder.PointerReleased += onReleased;

                    async void waitAndShow()
                    {
                        try
                        {
                            var token = cts.Token;
                            await Task.Delay(250, token);
                            ShowPeekViewer(e, ViewModel.PeekViewerSize * 2 + 1, token);
                        }
                        catch (Exception)
                        {
                            // Cancelled or disposed
                        }
                        cancel();
                    }

                    waitAndShow();
                }
            };

            if (zoomBorder != null && imageView != null)
            {
                zoomBorder.ZoomChanged += (s, e) =>
                {
                    if (!MathUtilities.AreClose(prevZoomX, e.ZoomX) || !MathUtilities.AreClose(prevZoomY, e.ZoomY))
                    {
                        prevZoomX = e.ZoomX;
                        prevZoomY = e.ZoomY;

                        if (ShouldAutoSetInterpolationMode())
                        {
                            UpdateInterpolationMode(e.ZoomX, e.ZoomY);
                        }
                    }
                };

                LayoutUpdated += (s, e) =>
                {
                    if (!prevBounds.HasValue || !prevBounds.Value.Equals(Bounds))
                    {
                        prevBounds = Bounds;

                        if (ShouldAutoSetInterpolationMode())
                        {
                            UpdateInterpolationMode(zoomBorder.ZoomX, zoomBorder.ZoomY);
                        }
                    }
                };
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            prevBounds = null;
            prevInterpolated = null;

            if (zoomBorder != null && imageView != null && ShouldAutoSetInterpolationMode())
            {
                UpdateInterpolationMode(zoomBorder.ZoomX, zoomBorder.ZoomY);
            }
        }

        private void ShowPeekViewer(PointerEventArgs e, int size, CancellationToken cancellationToken = default)
        {
            var vm = ViewModel?.PeekViewer;

            if (window != null && zoomBorder != null && imageView != null && vm != null)
            {
                FitsImagePeekViewerWindow window = new()
                {
                    DataContext = vm
                };

                var oldCursor = Cursor;
                var newCursor = new Cursor(StandardCursorType.None);
                Cursor = newCursor;
                window.Cursor = newCursor;

                void updateWindowPos(PointerEventArgs e)
                {
                    var pos = e.GetPosition(this.window);
                    window.Position = new PixelPoint(this.window.Position.X + (int)(pos.X - window.Width * 0.5), this.window.Position.Y + (int)(pos.Y - window.Height * 0.5));
                }

                void updateZoom(PointerEventArgs e)
                {
                    var relPos = e.GetPosition(imageView);
                    vm.Section = ImageSection.Dynamic(ImagePosition.TopLeft, ImagePosition.Center, ((int)relPos.X + 0.5) / imageView.Bounds.Width, ((int)relPos.Y + 0.5) / imageView.Bounds.Height, size, size, false);
                }

                updateWindowPos(e);

                void onReleased(object? sender, EventArgs e)
                {
                    window.Close();
                }

                var lastPointerPos = e.GetPosition(this);

                void onMoved(object? sender, PointerEventArgs e)
                {
                    updateWindowPos(e);
                    updateZoom(e);

                    var newPointerPos = e.GetPosition(this);

                    double dx = newPointerPos.X - lastPointerPos.X;
                    double dy = newPointerPos.Y - lastPointerPos.Y;
                    double delta = Math.Sqrt(dx * dx + dy * dy);

                    lastPointerPos = newPointerPos;

                    window.MovementOpacity = Math.Max(0.15f, (float)(window.MovementOpacity - delta / 14.0));
                }

                void cleanup()
                {
                    zoomBorder.PointerMoved -= onMoved;
                    zoomBorder.PointerReleased -= onReleased;
                    zoomBorder.PointerCaptureLost -= onReleased;
                    window.Closed -= onClosed;
                    if (Cursor == newCursor)
                    {
                        Cursor = oldCursor;
                    }
                    newCursor.Dispose();
                    isPeekViewerOpen = false;
                }

                void onClosed(object? sender, EventArgs e)
                {
                    cleanup();
                }
                window.Closed += onClosed;

                zoomBorder.PointerMoved += onMoved;
                zoomBorder.PointerReleased += onReleased;
                zoomBorder.PointerCaptureLost += onReleased;

                if (cancellationToken.IsCancellationRequested)
                {
                    cleanup();
                    throw new OperationCanceledException();
                }

                window.Show();

                isPeekViewerOpen = true;

                updateZoom(e);
            }
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            window = e.Root as Window;
        }

        private bool ShouldAutoSetInterpolationMode()
        {
            var vm = ViewModel;
            return vm != null && vm.AutoSetInterpolationMode;
        }

        private void UpdateInterpolationMode(double zoomX, double zoomY)
        {
            if (zoomBorder != null && imageView != null)
            {
                var vm = ViewModel;

                if (vm != null)
                {
                    var image = vm.FitsImage;

                    if (image != null && image.Bitmap != null)
                    {
                        double scaleX = zoomBorder.Bounds.Width / imageView.Bounds.Width;
                        double scaleY = zoomBorder.Bounds.Height / imageView.Bounds.Height;

                        double minScale = Math.Min(scaleX, scaleY);

                        double imageWidth = imageView.Bounds.Width * minScale * zoomX;
                        double imageHeight = imageView.Bounds.Height * minScale * zoomY;

                        var size = image.Bitmap.Size;

                        double ppx = size.Width / imageWidth;
                        double ppy = size.Height / imageHeight;

                        bool shouldInterpolate = Math.Max(ppx, ppy) > 0.5;

                        if (!prevInterpolated.HasValue || prevInterpolated.Value != shouldInterpolate)
                        {
                            prevInterpolated = shouldInterpolate;

                            if (shouldInterpolate)
                            {
                                image.InterpolationMode = Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality;
                            }
                            else
                            {
                                image.InterpolationMode = Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.Default;
                            }
                        }
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
