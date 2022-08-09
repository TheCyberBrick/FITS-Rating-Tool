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

namespace FitsRatingTool.GuiApp.UI.Controls
{
    public class AutoCanvas : Canvas
    {
        public static readonly StyledProperty<bool> AutoWidthProperty = AvaloniaProperty.Register<AutoCanvas, bool>(nameof(AutoWidth), true);

        public static readonly StyledProperty<bool> AutoHeightProperty = AvaloniaProperty.Register<AutoCanvas, bool>(nameof(AutoHeight), true);

        public static readonly DirectProperty<AutoCanvas, double> DesiredAutoWidthProperty = AvaloniaProperty.RegisterDirect<AutoCanvas, double>(nameof(DesiredAutoWidth), c => c.DesiredAutoWidth);

        public static readonly DirectProperty<AutoCanvas, double> DesiredAutoHeightProperty = AvaloniaProperty.RegisterDirect<AutoCanvas, double>(nameof(DesiredAutoHeight), c => c.DesiredAutoHeight);

        public bool AutoWidth
        {
            get => GetValue(AutoWidthProperty);
            set => SetValue(AutoWidthProperty, value);
        }

        public bool AutoHeight
        {
            get => GetValue(AutoHeightProperty);
            set => SetValue(AutoHeightProperty, value);
        }

        private double _desiredAutoWidth;
        public double DesiredAutoWidth
        {
            get => _desiredAutoWidth;
            set => SetAndRaise(DesiredAutoWidthProperty, ref _desiredAutoWidth, value);
        }

        private double _desiredAutoHeight;
        public double DesiredAutoHeight
        {
            get => _desiredAutoHeight;
            set => SetAndRaise(DesiredAutoHeightProperty, ref _desiredAutoHeight, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);

            double maxX = 0;
            double maxY = 0;

            foreach (Control child in Children)
            {
                double right = 0.0;
                double bottom = 0.0;
                double elementLeft = GetLeft(child);
                var desiredSize = child.DesiredSize;

                if (!double.IsNaN(elementLeft))
                {
                    right = elementLeft + desiredSize.Width;
                }
                else
                {
                    double elementRight = GetRight(child);
                    if (!double.IsNaN(elementRight))
                    {
                        right = child.DesiredSize.Width;
                    }
                }

                double elementTop = GetTop(child);
                if (!double.IsNaN(elementTop))
                {
                    bottom = elementTop + desiredSize.Height;
                }
                else
                {
                    double elementBottom = GetBottom(child);
                    if (!double.IsNaN(elementBottom))
                    {
                        bottom = child.DesiredSize.Height;
                    }
                }

                maxX = Math.Max(maxX, right);
                maxY = Math.Max(maxY, bottom);
            }

            var autoSize = new Size(Math.Min(availableSize.Width, maxX), Math.Min(availableSize.Height, maxY));

            DesiredAutoWidth = autoSize.Width;
            DesiredAutoHeight = autoSize.Height;

            var finalSize = new Size();

            if (AutoWidth)
            {
                finalSize = new Size(autoSize.Width, finalSize.Height);
            }

            if (AutoHeight)
            {
                finalSize = new Size(finalSize.Width, autoSize.Height);
            }

            return finalSize;
        }
    }
}
