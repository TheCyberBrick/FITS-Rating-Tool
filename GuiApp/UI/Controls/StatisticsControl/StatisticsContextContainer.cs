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
using Avalonia.Controls.Mixins;
using Avalonia.Data;
using System;
using System.Collections.Generic;
using FitsRatingTool.GuiApp.UI.Controls.ContextualItemsControl;

namespace FitsRatingTool.GuiApp.UI.Controls.StatisticsControl
{
    public class StatisticsContextContainer : ContextContainer, IStatisticsContextContainer
    {
        #region +++ Input Bindings +++
        public static readonly DirectProperty<StatisticsContextContainer, Dictionary<string, IBinding>> ValueBindingsProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, Dictionary<string, IBinding>>(nameof(ValueBindings), o => o.ValueBindings, (o, v) => o.ValueBindings = v, null!, BindingMode.OneWay);

        public static readonly DirectProperty<StatisticsContextContainer, Dictionary<string, IBinding>> EnabledBindingsProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, Dictionary<string, IBinding>>(nameof(EnabledBindings), o => o.EnabledBindings, (o, v) => o.EnabledBindings = v, null!, BindingMode.OneWay);

        public static readonly DirectProperty<StatisticsContextContainer, object?> InputValueProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, object?>(nameof(InputValue), o => o.InputValue, (o, v) => o.InputValue = v, null, BindingMode.OneWay);

        public static readonly DirectProperty<StatisticsContextContainer, bool> InputEnabledProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, bool>(nameof(InputEnabled), o => o.InputEnabled, (o, v) => o.InputEnabled = v, false, BindingMode.OneWay);

        private Dictionary<string, IBinding> _valueBindings = new();
        public Dictionary<string, IBinding> ValueBindings
        {
            get => _valueBindings;
            set => SetAndRaise(ValueBindingsProperty, ref _valueBindings, value);
        }

        private Dictionary<string, IBinding> _enabledBindings = new();
        public Dictionary<string, IBinding> EnabledBindings
        {
            get => _enabledBindings;
            set => SetAndRaise(ValueBindingsProperty, ref _enabledBindings, value);
        }

        private object? _inputValue;
        public object? InputValue
        {
            get => _inputValue;
            set => SetAndRaise(InputValueProperty, ref _inputValue, value);
        }

        private bool _inputEnabled;
        public bool InputEnabled
        {
            get => _inputEnabled;
            set => SetAndRaise(InputEnabledProperty, ref _inputEnabled, value);
        }
        #endregion

        #region +++ Output Bindings +++
        public static readonly DirectProperty<StatisticsContextContainer, object?> ValueProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, object?>(nameof(Value), o => o.Value, (o, v) => o.Value = v, null, BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, bool> EnabledProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, bool>(nameof(Enabled), o => o.Enabled, (o, v) => o.Enabled = v, false, BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, double> XProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, double>(nameof(X), o => o.X, (o, v) => o.X = v, 0.0, BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, double> YProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, double>(nameof(Y), o => o.Y, (o, v) => o.Y = v, 0.0, BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, double> CanvasXProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, double>(nameof(CanvasX), o => o.CanvasX, (o, v) => o.CanvasX = v, 0.0, BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, double> CanvasYProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, double>(nameof(CanvasY), o => o.CanvasY, (o, v) => o.CanvasY = v, 0.0, BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, double> CanvasWidthProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, double>(nameof(CanvasWidth), o => o.CanvasWidth, (o, v) => o.CanvasWidth = v, 0.0, BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, double> CanvasHeightProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, double>(nameof(CanvasHeight), o => o.CanvasHeight, (o, v) => o.CanvasHeight = v, 0.0, BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, Point> StartPointProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, Point>(nameof(StartPoint), o => o.StartPoint, (o, v) => o.StartPoint = v, new Point(0, 0), BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, Point> EndPointProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, Point>(nameof(EndPoint), o => o.EndPoint, (o, v) => o.EndPoint = v, new Point(0, 0), BindingMode.OneWayToSource);

        public static readonly DirectProperty<StatisticsContextContainer, bool> HasPreviousPointProperty =
            AvaloniaProperty.RegisterDirect<StatisticsContextContainer, bool>(nameof(HasPreviousPoint), o => o.HasPreviousPoint, (o, v) => o.HasPreviousPoint = v, false, BindingMode.OneWayToSource);

        private object? _value;
        public object? Value
        {
            get => _value;
            set => SetAndRaise(ValueProperty, ref _value, value);
        }

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set => SetAndRaise(EnabledProperty, ref _enabled, value);
        }

        private double _x;
        public double X
        {
            get => _x;
            set => SetAndRaise(XProperty, ref _x, value);
        }

        private double _y;
        public double Y
        {
            get => _y;
            set => SetAndRaise(YProperty, ref _y, value);
        }

        private double _canvasX;
        public double CanvasX
        {
            get => _canvasX;
            set => SetAndRaise(CanvasXProperty, ref _canvasX, value);
        }

        private double _canvasY;
        public double CanvasY
        {
            get => _canvasY;
            set => SetAndRaise(CanvasYProperty, ref _canvasY, value);
        }

        private double _canvasWidth;
        public double CanvasWidth
        {
            get => _canvasWidth;
            set => SetAndRaise(CanvasWidthProperty, ref _canvasWidth, value);
        }

        private double _canvasHeight;
        public double CanvasHeight
        {
            get => _canvasHeight;
            set => SetAndRaise(CanvasHeightProperty, ref _canvasHeight, value);
        }

        private Point _startPoint;
        public Point StartPoint
        {
            get => _startPoint;
            set => SetAndRaise(StartPointProperty, ref _startPoint, value);
        }

        private Point _endPoint;
        public Point EndPoint
        {
            get => _endPoint;
            set => SetAndRaise(EndPointProperty, ref _endPoint, value);
        }

        public bool _hasPreviousPoint;
        public bool HasPreviousPoint
        {
            get => _hasPreviousPoint;
            set => SetAndRaise(HasPreviousPointProperty, ref _hasPreviousPoint, value);
        }
        #endregion


        public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<StatisticsContextContainer, bool>(nameof(IsSelected));

        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        static StatisticsContextContainer()
        {
            SelectableMixin.Attach<StatisticsContextContainer>(IsSelectedProperty);
            PressedMixin.Attach<StatisticsContextContainer>();
            FocusableProperty.OverrideDefaultValue<StatisticsContextContainer>(true);
        }

        private IDisposable? valueBindingDisposable;
        private IDisposable? enabledBindingDisposable;

        private IBinding? GetValueBinding(string? dataKey)
        {
            IBinding? binding = null;
            if (dataKey != null && ValueBindings != null)
            {
                ValueBindings.TryGetValue(dataKey, out binding);
            }
            return binding;
        }
        private IBinding? GetEnabledBinding(string? dataKey)
        {
            IBinding? binding = null;
            if (dataKey != null && EnabledBindings != null)
            {
                EnabledBindings.TryGetValue(dataKey, out binding);
            }
            return binding;
        }

        public void BindToDataKey(string? dataKey)
        {
            Unbind();

            if (dataKey == null)
            {
                InputValue = null;
                InputEnabled = false;
                return;
            }

            var valueBinding = GetValueBinding(dataKey);
            if (valueBinding != null)
            {
                valueBindingDisposable = this.Bind(InputValueProperty, valueBinding);

                var enabledBinding = GetEnabledBinding(dataKey);
                if (enabledBinding != null)
                {
                    enabledBindingDisposable = this.Bind(InputEnabledProperty, enabledBinding);
                }
                else
                {
                    InputEnabled = false;
                }
            }
            else
            {
                InputValue = null;
                InputEnabled = false;
            }
        }

        public void Unbind()
        {
            valueBindingDisposable?.Dispose();
            valueBindingDisposable = null;

            enabledBindingDisposable?.Dispose();
            enabledBindingDisposable = null;
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == InputValueProperty)
            {
                Value = change.NewValue.Value;
            }
            else if (change.Property == InputEnabledProperty)
            {
                Enabled = Convert.ToBoolean(change.NewValue.Value);
            }
        }
    }
}
