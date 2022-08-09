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
using System.Globalization;

namespace FitsRatingTool.GuiApp.UI.Converters
{
    public static class ThicknessConverter
    {
        private class MaxConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                Thickness? thickness1 = value as Thickness?;
                Thickness? thickness2 = parameter as Thickness?;

                if (thickness1.HasValue && !thickness2.HasValue)
                {
                    return thickness1;
                }
                else if (!thickness1.HasValue && thickness2.HasValue)
                {
                    return thickness2;
                }
                else if (thickness1.HasValue && thickness2.HasValue)
                {
                    return new Thickness(
                        Math.Max(thickness1.Value.Left, thickness2.Value.Left),
                        Math.Max(thickness1.Value.Top, thickness2.Value.Top),
                        Math.Max(thickness1.Value.Right, thickness2.Value.Right),
                        Math.Max(thickness1.Value.Bottom, thickness2.Value.Bottom)
                        );
                }

                return new Thickness(0);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private class MinConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                Thickness? thickness1 = value as Thickness?;
                Thickness? thickness2 = parameter as Thickness?;

                if (thickness1.HasValue && !thickness2.HasValue)
                {
                    return thickness1;
                }
                else if (!thickness1.HasValue && thickness2.HasValue)
                {
                    return thickness2;
                }
                else if (thickness1.HasValue && thickness2.HasValue)
                {
                    return new Thickness(
                        Math.Min(thickness1.Value.Left, thickness2.Value.Left),
                        Math.Min(thickness1.Value.Top, thickness2.Value.Top),
                        Math.Min(thickness1.Value.Right, thickness2.Value.Right),
                        Math.Min(thickness1.Value.Bottom, thickness2.Value.Bottom)
                        );
                }

                return new Thickness(0);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public static readonly IValueConverter TopToDouble = new FuncValueConverter<Thickness, double>(x => x.Top);
        public static readonly IValueConverter RightToDouble = new FuncValueConverter<Thickness, double>(x => x.Right);
        public static readonly IValueConverter BottomToDouble = new FuncValueConverter<Thickness, double>(x => x.Bottom);
        public static readonly IValueConverter LeftToDouble = new FuncValueConverter<Thickness, double>(x => x.Left);
        public static readonly IValueConverter Min = new MinConverter();
        public static readonly IValueConverter Max = new MaxConverter();
    }
}
