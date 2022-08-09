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
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Windows.Input;

namespace FitsRatingTool.GuiApp.UI.Controls
{
    public class ClickAction : AvaloniaObject
    {
        public static readonly AttachedProperty<MouseButton> ButtonProperty = AvaloniaProperty.RegisterAttached<ClickAction, Interactive, MouseButton>("Button", MouseButton.Left, false, BindingMode.OneWay);

        public static readonly AttachedProperty<ICommand?> CommandProperty = AvaloniaProperty.RegisterAttached<ClickAction, Interactive, ICommand?>("Command", default!, false, BindingMode.OneWay);

        public static readonly AttachedProperty<object?> CommandParameterProperty = AvaloniaProperty.RegisterAttached<ClickAction, Interactive, object?>("CommandParameter", default!, false, BindingMode.OneWay);

        static ClickAction()
        {
            CommandProperty.Changed.Subscribe(x => HandleCommandChanged(x.Sender, x.NewValue.GetValueOrDefault<ICommand>()));
        }

        private static void HandleCommandChanged(IAvaloniaObject element, ICommand? commandValue)
        {
            if (element is Interactive interactElem)
            {
                if (commandValue != null)
                {
                    interactElem.AddHandler(InputElement.PointerPressedEvent, Handler);
                }
                else
                {
                    interactElem.RemoveHandler(InputElement.PointerPressedEvent, Handler);
                }
            }

            void Handler(object? s, PointerPressedEventArgs e)
            {
                if (interactElem.GetValue(ButtonProperty) == e.GetCurrentPoint(null).Properties.PointerUpdateKind.GetMouseButton())
                {
                    var commandParameter = interactElem.GetValue(CommandParameterProperty);
                    if (commandValue?.CanExecute(commandParameter) == true)
                    {
                        commandValue.Execute(commandParameter);
                    }
                }
            }
        }

        public static void SetButton(AvaloniaObject element, MouseButton button)
        {
            element.SetValue(ButtonProperty, button);
        }

        public static MouseButton GetButton(AvaloniaObject element)
        {
            return element.GetValue(ButtonProperty);
        }

        public static void SetCommand(AvaloniaObject element, ICommand commandValue)
        {
            element.SetValue(CommandProperty, commandValue);
        }

        public static ICommand? GetCommand(AvaloniaObject element)
        {
            return element.GetValue(CommandProperty);
        }

        public static void SetCommandParameter(AvaloniaObject element, object parameter)
        {
            element.SetValue(CommandParameterProperty, parameter);
        }

        public static object? GetCommandParameter(AvaloniaObject element)
        {
            return element.GetValue(CommandParameterProperty);
        }
    }
}
