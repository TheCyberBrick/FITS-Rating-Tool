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
using Avalonia.Controls.Generators;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using FitsRatingTool.GuiApp.UI.Controls.ContextualItemsControl;

namespace FitsRatingTool.GuiApp.UI.Controls.StatisticsControl
{
    public class StatisticsControl : ContextualItemsControl<IStatisticsContextContainer, StatisticsContextContainer>
    {
        public static readonly StyledProperty<string?> DataKeyProperty = AvaloniaProperty.Register<StatisticsControl, string?>(nameof(DataKey));

        public static readonly StyledProperty<ScalingMode> ScalingModeProperty = AvaloniaProperty.Register<StatisticsControl, ScalingMode>(nameof(ScalingMode), ScalingMode.Auto);

        public static readonly StyledProperty<double> GraphMinProperty = AvaloniaProperty.Register<StatisticsControl, double>(nameof(GraphMin), 0.0);

        public static readonly StyledProperty<double> GraphMaxProperty = AvaloniaProperty.Register<StatisticsControl, double>(nameof(GraphMax), 0.0);

        public string? DataKey
        {
            get { return GetValue(DataKeyProperty); }
            set { SetValue(DataKeyProperty, value); }
        }

        public ScalingMode ScalingMode
        {
            get { return GetValue(ScalingModeProperty); }
            set { SetValue(ScalingModeProperty, value); }
        }

        public double GraphMin
        {
            get { return GetValue(GraphMinProperty); }
            set { SetValue(GraphMinProperty, value); }
        }

        public double GraphMax
        {
            get { return GetValue(GraphMaxProperty); }
            set { SetValue(GraphMaxProperty, value); }
        }


        private Size? prevSize;
        private bool isContextDirty = false;

        static StatisticsControl()
        {
            DataKeyProperty.Changed.AddClassHandler<StatisticsControl>((x, e) => x.DataKeyChanged(e));

            AffectsArrange<StatisticsControl>(DataKeyProperty, ScalingModeProperty, GraphMinProperty, GraphMaxProperty);
        }

        protected static void BindContainerValueToDataKey(IStatisticsContextContainer container, string? dataKey)
        {
            container.BindToDataKey(dataKey);
        }

        protected virtual void DataKeyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            string? newDataKey = (string?)e.NewValue;

            foreach (var child in LogicalChildren)
            {
                if (child is IStatisticsContextContainer container)
                {
                    BindContainerValueToDataKey(container, newDataKey);
                }
            }
        }

        protected override void OnContainersMaterialized(ItemContainerEventArgs e)
        {
            base.OnContainersMaterialized(e);

            var dataKey = DataKey;

            foreach (var info in e.Containers)
            {
                if (info.ContainerControl is IStatisticsContextContainer container)
                {
                    BindContainerValueToDataKey(container, dataKey);
                }
            }

            if (!isContextDirty)
            {
                isContextDirty = true;
                InvalidateArrange();
            }
        }

        protected override void OnContainersDematerialized(ItemContainerEventArgs e)
        {
            base.OnContainersDematerialized(e);

            foreach (var info in e.Containers)
            {
                if (info.ContainerControl is IStatisticsContextContainer container)
                {
                    // Dispose the binding
                    BindContainerValueToDataKey(container, null);
                }
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            finalSize = base.ArrangeOverride(finalSize);
            if (isContextDirty || prevSize.HasValue && !prevSize.Value.NearlyEquals(finalSize))
            {
                prevSize = finalSize;

                // Delay for a tick so that multiple updates
                // coming in at once are batched together
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateAllContexts(finalSize);
                    isContextDirty = false;
                }, DispatcherPriority.Layout);
            }
            return finalSize;
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> args)
        {
            base.OnPropertyChanged(args);
            if (!isContextDirty &&
                (args.Property == DataKeyProperty
                || args.Property == ScalingModeProperty
                || args.Property == GraphMinProperty
                || args.Property == GraphMaxProperty))
            {
                isContextDirty = true;
                InvalidateArrange();
            }
        }

        protected override void OnContainerPropertyChanged(object? item, int index, IStatisticsContextContainer context, AvaloniaPropertyChangedEventArgs args)
        {
            base.OnContainerPropertyChanged(item, index, context, args);
            if (!isContextDirty &&
                (args.Property == StatisticsContextContainer.ValueBindingsProperty
                || args.Property == StatisticsContextContainer.EnabledBindingsProperty
                || args.Property == StatisticsContextContainer.InputValueProperty
                || args.Property == StatisticsContextContainer.InputEnabledProperty
                || args.Property == StatisticsContextContainer.ValueProperty
                || args.Property == StatisticsContextContainer.EnabledProperty))
            {
                isContextDirty = true;
                InvalidateArrange();
            }
        }

        protected void UpdateAllContexts(Size size)
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            int n = 0;

            foreach (var record in GetContextRecords())
            {
                if (record.Context.Enabled)
                {
                    double value = Convert.ToDouble(record.Context.Value);
                    minValue = Math.Min(minValue, value);
                    maxValue = Math.Max(maxValue, value);
                }
                ++n;
            }

            if (ScalingMode != ScalingMode.Auto)
            {
                minValue = GraphMin;
                maxValue = GraphMax;
            }

            double px = 0;
            double py = 0;

            ContextRecord? prevRecord = null;

            foreach (var record in GetContextRecords())
            {
                if (record.Context.Enabled)
                {
                    record.Context.X = n == 1 ? (size.Width - Padding.Right - Padding.Left) * 0.5 : (size.Width - Padding.Right - Padding.Left) / (n - 1) * record.Index;
                    record.Context.Y = (size.Height - Padding.Top - Padding.Bottom) * (1 - (Convert.ToDouble(record.Context.Value) - minValue) / (maxValue - minValue));
                    if (!double.IsFinite(record.Context.Y))
                    {
                        record.Context.Y = 0;
                    }

                    record.Context.CanvasX = record.Context.X - 500;
                    record.Context.CanvasY = record.Context.Y - 500;
                    record.Context.CanvasWidth = 1000;
                    record.Context.CanvasHeight = 1000;

                    record.Context.StartPoint = new Point(px, py);
                    record.Context.EndPoint = new Point(record.Context.X, record.Context.Y);

                    px = record.Context.X;
                    py = record.Context.Y;

                    record.Context.HasPreviousPoint = prevRecord.HasValue;

                    prevRecord = record;
                }
            }
        }


        #region +++ ListBox Selection Behaviour +++
        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                e.Handled = UpdateSelectionFromEventSource(
                    e.Source,
                    true,
                    e.KeyModifiers.HasAllFlags(KeyModifiers.Shift),
                    e.KeyModifiers.HasAllFlags(KeyModifiers.Control));
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.Source is IVisual source)
            {
                var point = e.GetCurrentPoint(source);

                if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
                {
                    e.Handled = UpdateSelectionFromEventSource(
                        e.Source,
                        true,
                        e.KeyModifiers.HasAllFlags(KeyModifiers.Shift),
                        e.KeyModifiers.HasAllFlags(KeyModifiers.Control),
                        point.Properties.IsRightButtonPressed);
                }
            }
        }
        #endregion
    }
}
