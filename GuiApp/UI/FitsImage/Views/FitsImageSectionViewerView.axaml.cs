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
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System;
using FitsRatingTool.GuiApp.UI.FitsImage.ViewModels;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Media;
using Avalonia.Visuals.Media.Imaging;
using Avalonia;
using static FitsRatingTool.GuiApp.UI.FitsImage.IFitsImageSectionViewerViewModel;
using System.Reactive.Disposables;

namespace FitsRatingTool.GuiApp.UI.FitsImage.Views
{
    public partial class FitsImageSectionViewerView : ReactiveUserControl<FitsImageSectionViewerViewModel>
    {
        private ZoomBorder zoomBorder;
        private Image image;

        private ImageSection? section;

        public FitsImageSectionViewerView()
        {
            InitializeComponent();

            zoomBorder = this.Find<ZoomBorder>("ZoomBorder");
            if (zoomBorder == null)
            {
                throw new InvalidOperationException("Unable to find ZoomBorder");
            }

            image = this.Find<Image>("Image");
            if (image == null)
            {
                throw new InvalidOperationException("Unable to find Image");
            }

            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Section)
                     .Subscribe(x => UpdateZoom(Bounds.Size, x))
                     .DisposeWith(d);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            finalSize = base.ArrangeOverride(finalSize);
            if (section.HasValue)
            {
                UpdateZoom(finalSize, section.Value);
            }
            return finalSize;
        }

        private void UpdateZoom(Size size, ImageSection section)
        {
            this.section = section;

            if (section.IsFixedZoom)
            {
                SetZoom(section.Origin, section.Target, section.FixedZoomMagnification, section.X, section.Y);
            }
            else
            {
                if (ViewModel != null)
                {
                    var img = ViewModel.Image.Bitmap;

                    if (img != null)
                    {
                        double scaleX = zoomBorder.Bounds.Width / img.Size.Width;
                        double scaleY = zoomBorder.Bounds.Height / img.Size.Height;

                        double scale = Math.Min(scaleX, scaleY);

                        double zoomX = zoomBorder.Bounds.Width / (section.DynamicZoomSizeX * scale);
                        double zoomY = zoomBorder.Bounds.Height / (section.DynamicZoomSizeY * scale);

                        double zoom = section.IsDynamicZoomSizeMin ? Math.Min(zoomX, zoomY) : Math.Max(zoomX, zoomY);

                        SetZoom(section.Origin, section.Target, zoom, section.X, section.Y);
                    }
                }
            }
        }

        private void SetZoom(ImagePosition origin, ImagePosition target, double zoom, double x, double y)
        {
            switch (origin)
            {
                case ImagePosition.Top:
                    x += 0.5;
                    break;
                case ImagePosition.Right:
                    x += 1.0;
                    y += 0.5;
                    break;
                case ImagePosition.Bottom:
                    x += 0.5;
                    y += 1.0;
                    break;
                case ImagePosition.Left:
                    y += 0.5;
                    break;
                case ImagePosition.TopLeft:
                    break;
                case ImagePosition.TopRight:
                    x += 1.0;
                    break;
                case ImagePosition.BottomLeft:
                    y += 1.0;
                    break;
                case ImagePosition.BottomRight:
                    x += 1.0;
                    y += 1.0;
                    break;
                case ImagePosition.Center:
                    x += 0.5;
                    y += 0.5;
                    break;
            }

            double scaleX = zoomBorder.Bounds.Width / image.Bounds.Width;
            double scaleY = zoomBorder.Bounds.Height / image.Bounds.Height;

            double minScale = Math.Min(scaleX, scaleY);
            scaleX /= minScale;
            scaleY /= minScale;

            var relSize = 0.5 / zoom;
            var relSizeX = relSize * scaleX;
            var relSizeY = relSize * scaleY;

            switch (target)
            {
                case ImagePosition.Top:
                    y += relSizeY;
                    break;
                case ImagePosition.Right:
                    x -= relSizeY;
                    break;
                case ImagePosition.Bottom:
                    y -= relSizeY;
                    break;
                case ImagePosition.Left:
                    x += relSizeY;
                    break;
                case ImagePosition.TopLeft:
                    x += relSizeX;
                    y += relSizeY;
                    break;
                case ImagePosition.TopRight:
                    x -= relSizeX;
                    y += relSizeY;
                    break;
                case ImagePosition.BottomLeft:
                    x += relSizeX;
                    y -= relSizeY;
                    break;
                case ImagePosition.BottomRight:
                    x -= relSizeX;
                    y -= relSizeY;
                    break;
                case ImagePosition.Center:
                    break;
            }

            zoomBorder.Zoom(zoom, 0, 0, true);
            zoomBorder.Pan(zoomBorder.Bounds.Width * 0.5 - image.Bounds.Left - x * image.Bounds.Width * zoom, zoomBorder.Bounds.Height * 0.5 - image.Bounds.Top - y * image.Bounds.Height * zoom, true);

            double pixelsPerUnit;

            if (image.Source != null)
            {
                double imageWidth = image.Bounds.Width * minScale * zoom;
                double imageHeight = image.Bounds.Height * minScale * zoom;

                var size = image.Source.Size;

                double ppx = size.Width / imageWidth;
                double ppy = size.Height / imageHeight;

                pixelsPerUnit = Math.Max(ppx, ppy);
            }
            else
            {
                pixelsPerUnit = 1;
            }

            if (pixelsPerUnit <= 0.5)
            {
                image[RenderOptions.BitmapInterpolationModeProperty] = BitmapInterpolationMode.Default;
            }
            else
            {
                image[RenderOptions.BitmapInterpolationModeProperty] = BitmapInterpolationMode.HighQuality;
            }
        }
    }
}
