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

using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FitsRatingTool.GuiApp.UI.Converters
{
    public static class MathConverters
    {
        private class AddConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToDouble(value) - System.Convert.ToDouble(parameter);
            }
        }

        private class MaxConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                double d = System.Convert.ToDouble(value);
                double a = System.Convert.ToDouble(parameter);
                if (!double.IsFinite(d))
                {
                    return a;
                }
                return Math.Max(d, a);
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
                double d = System.Convert.ToDouble(value);
                double a = System.Convert.ToDouble(parameter);
                if (!double.IsFinite(d))
                {
                    return a;
                }
                return Math.Min(d, a);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        private class MultiplicationConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToDouble(value) / System.Convert.ToDouble(parameter);
            }
        }
        private class LongMultiplicationConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToInt64(value) * System.Convert.ToInt64(parameter);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToInt64(value) / System.Convert.ToInt64(parameter);
            }
        }
        private class DivisionConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToDouble(value) / System.Convert.ToDouble(parameter);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
            }
        }
        private class LongDivisionConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToInt64(value) / System.Convert.ToInt64(parameter);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToInt64(value) * System.Convert.ToInt64(parameter);
            }
        }

        public static readonly IValueConverter Add = new AddConverter();
        public static readonly IValueConverter Max = new MaxConverter();
        public static readonly IValueConverter Min = new MinConverter();
        public static readonly IValueConverter Multiplication = new MultiplicationConverter();
        public static readonly IValueConverter LongMultiplication = new LongMultiplicationConverter();
        public static readonly IValueConverter Division = new DivisionConverter();
        public static readonly IValueConverter LongDivision = new LongDivisionConverter();
    }
}
