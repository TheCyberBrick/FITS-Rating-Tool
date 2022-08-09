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
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Utilities;
using System;

namespace FitsRatingTool.GuiApp.UI.Controls
{
    internal class PrecisionSliderTemplateControl : TemplatedControl
    {
        public static readonly DirectProperty<PrecisionSliderTemplateControl, double> PrecisionValueProperty = PrecisionSlider.PrecisionValueProperty.AddOwner<PrecisionSliderTemplateControl>(
                o => o.PrecisionValue,
                (o, v) => o.PrecisionValue = v,
                0.5,
                defaultBindingMode: BindingMode.TwoWay);

        private double _precisionValue;
        public double PrecisionValue
        {
            get => _precisionValue;
            set => SetAndRaise(PrecisionValueProperty, ref _precisionValue, value);
        }

        private readonly PrecisionSlider precisionSlider;

        public PrecisionSliderTemplateControl(PrecisionSlider precisionSlider) : base()
        {
            this.precisionSlider = precisionSlider;
        }
    }

    [PseudoClasses(":flyout-open")]
    public class PrecisionSlider : Slider
    {
        public static readonly DirectProperty<PrecisionSlider, double> PrecisionValueProperty =
            AvaloniaProperty.RegisterDirect<PrecisionSlider, double>(
                nameof(PrecisionValue),
                o => o.PrecisionValue,
                (o, v) => o.PrecisionValue = v,
                0.5,
                defaultBindingMode: BindingMode.TwoWay);

        private double _precisionValue = 0.5;
        public double PrecisionValue
        {
            get
            {
                return _precisionValue;
            }

            set
            {
                if (double.IsInfinity(value) || double.IsNaN(value))
                {
                    return;
                }

                if (IsInitialized)
                {
                    value = MathUtilities.Clamp(value, 0.0, 1.0);
                    SetAndRaise(PrecisionValueProperty, ref _precisionValue, value);
                }
                else
                {
                    SetAndRaise(PrecisionValueProperty, ref _precisionValue, value);
                }
            }
        }


        public static readonly StyledProperty<double> PrecisionScaleProperty =
            AvaloniaProperty.Register<PrecisionSlider, double>(nameof(PrecisionScale), 0.0025f);

        public double PrecisionScale
        {
            get => GetValue(PrecisionScaleProperty);
            set => SetValue(PrecisionScaleProperty, value);
        }


        public static readonly StyledProperty<FlyoutBase?> FlyoutProperty =
            AvaloniaProperty.Register<Button, FlyoutBase?>(nameof(Flyout));

        public FlyoutBase? Flyout
        {
            get => GetValue(FlyoutProperty);
            set => SetValue(FlyoutProperty, value);
        }


        public static readonly StyledProperty<IControlTemplate?> PrecisionSliderTemplateProperty =
            AvaloniaProperty.Register<PrecisionSlider, IControlTemplate?>(nameof(Template));

        public IControlTemplate? PrecisionSliderTemplate
        {
            get { return GetValue(PrecisionSliderTemplateProperty); }
            set { SetValue(PrecisionSliderTemplateProperty, value); }
        }


        public static readonly StyledProperty<Classes?> FlyoutPresenterClassesProperty =
            AvaloniaProperty.Register<PrecisionSlider, Classes?>(nameof(FlyoutPresenterClasses));

        public Classes? FlyoutPresenterClasses
        {
            get { return GetValue(FlyoutPresenterClassesProperty); }
            set { SetValue(FlyoutPresenterClassesProperty, value); }
        }


        private PrecisionSliderTemplateControl precisionSliderTemplateControl;

        public IControl? PrecisionControl
        {
            get;
            private set;
        }

        private IDisposable? precisionControlPointerReleaseDispose;



        private bool isFlyoutOpen = false;

        private bool updatePrecisionDelta = true;



        public PrecisionSlider()
        {
            AddHandler(PointerPressedEvent, OnPointerPressedTunneled, RoutingStrategies.Tunnel);

            precisionSliderTemplateControl = new PrecisionSliderTemplateControl(this)
            {
                [!DataContextProperty] = this[!DataContextProperty],
                [!TemplateProperty] = this[!PrecisionSliderTemplateProperty],
                [!PrecisionSliderTemplateControl.PrecisionValueProperty] = this[!PrecisionValueProperty]
            };
            precisionSliderTemplateControl.TemplateApplied += OnPrecisionSliderTemplateControlApplied;
        }


        private void RegisterFlyoutEvents(FlyoutBase? flyout)
        {
            if (flyout != null)
            {
                flyout.Opened += Flyout_Opened;
                flyout.Closed += Flyout_Closed;
            }
        }

        private void UnregisterFlyoutEvents(FlyoutBase? flyout)
        {
            if (flyout != null)
            {
                flyout.Opened -= Flyout_Opened;
                flyout.Closed -= Flyout_Closed;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            UnregisterFlyoutEvents(Flyout);
            RegisterFlyoutEvents(Flyout);
            UpdatePseudoClasses();

            precisionControlPointerReleaseDispose?.Dispose();
            PrecisionControl = e.NameScope.Find<IControl>("PART_PrecisionControl");
            if (PrecisionControl != null)
            {
                precisionControlPointerReleaseDispose = PrecisionControl.AddDisposableHandler(PointerReleasedEvent, PrecisionControlReleased, RoutingStrategies.Tunnel);
            }
        }

        private void OnPrecisionSliderTemplateControlApplied(object? sender, TemplateAppliedEventArgs args)
        {
            var newPrecisionControl = args.NameScope.Find<IControl>("PART_PrecisionControl");
            if (newPrecisionControl != null)
            {
                precisionControlPointerReleaseDispose?.Dispose();
                PrecisionControl = newPrecisionControl;
                precisionControlPointerReleaseDispose = PrecisionControl.AddDisposableHandler(PointerReleasedEvent, PrecisionControlReleased, RoutingStrategies.Tunnel);
            }
        }

        private void UpdateFlyoutContent()
        {
            if (Flyout == null)
            {
                Flyout = new Flyout();
            }
            if (Flyout is Flyout regularFlyout)
            {
                regularFlyout.Content = precisionSliderTemplateControl!;
            }
        }

        private void OnPointerPressedTunneled(object? sender, PointerPressedEventArgs e)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsRightButtonPressed || properties.IsMiddleButtonPressed)
            {
                //Flyout?.ShowAt(this, true);
                Flyout?.ShowAt(this, false);
                UpdateFlyoutContent();
                e.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsRightButtonPressed || properties.IsMiddleButtonPressed)
            {
                //Flyout?.ShowAt(this, true);
                Flyout?.ShowAt(this, false);
                UpdateFlyoutContent();
                e.Handled = true;
            }
        }

        private void PrecisionControlReleased(object? sender, PointerReleasedEventArgs args)
        {
            if (precisionSliderTemplateControl != null)
            {
                updatePrecisionDelta = false;
                precisionSliderTemplateControl.PrecisionValue = 0.5f;
                updatePrecisionDelta = true;
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PrecisionValueProperty)
            {
                if (updatePrecisionDelta && change.NewValue.HasValue && change.OldValue.HasValue)
                {
                    var precisionChange = change as AvaloniaPropertyChangedEventArgs<double>;
                    double delta = precisionChange!.NewValue.Value - precisionChange!.OldValue.Value;
                    Value += delta * PrecisionScale;
                }
            }
            else if (change.Property == FlyoutProperty)
            {
                var flyoutChange = change as AvaloniaPropertyChangedEventArgs<FlyoutBase?>;

                var oldFlyout = flyoutChange!.OldValue.GetValueOrDefault(null);
                var newFlyout = flyoutChange!.NewValue.GetValueOrDefault(null);

                // If flyout is changed while one is already open, make sure we 
                // close the old one first
                if (oldFlyout != null && oldFlyout.IsOpen)
                {
                    oldFlyout.Hide();
                }

                // Must unregister events here while a reference to the old flyout still exists
                UnregisterFlyoutEvents(oldFlyout);

                RegisterFlyoutEvents(newFlyout);
                UpdatePseudoClasses();
            }
        }

        private void Flyout_Opened(object? sender, EventArgs e)
        {
            var flyout = sender as FlyoutBase;

            // It is possible to share flyouts among multiple controls including Button.
            // This can cause a problem here since all controls that share a flyout receive
            // the same Opened/Closed events at the same time.
            // For Button that means they all would be updating their pseudoclasses accordingly.
            // In other words, all Buttons with a shared Flyout would have the backgrounds changed together.
            // To fix this, only continue here if the Flyout target matches this Button instance.
            if (ReferenceEquals(flyout?.Target, this))
            {
                isFlyoutOpen = true;
                UpdatePseudoClasses();
            }
        }

        private void Flyout_Closed(object? sender, EventArgs e)
        {
            var flyout = sender as FlyoutBase;

            // See comments in Flyout_Opened
            if (ReferenceEquals(flyout?.Target, this))
            {
                isFlyoutOpen = false;
                UpdatePseudoClasses();
            }
        }

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":flyout-open", isFlyoutOpen);
        }
    }
}
