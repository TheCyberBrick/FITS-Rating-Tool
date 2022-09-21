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
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FitsRatingTool.GuiApp.UI.Converters
{
    public static class ConditionalConverters
    {
        private class IfElseConverter : IMultiValueConverter
        {
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (values.Count != 3)
                {
                    return AvaloniaProperty.UnsetValue;
                }

                var v = values[0];

                if (v != null && bool.TryParse(v.ToString(), out var val) && val)
                {
                    return values[1];
                }
                else
                {
                    return values[2];
                }
            }
        }

        public static readonly IMultiValueConverter IfElse = new IfElseConverter();
    }
}
