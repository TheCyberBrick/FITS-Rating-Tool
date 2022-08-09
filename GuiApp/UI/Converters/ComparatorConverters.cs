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
    public class ComparatorConverters
    {
        public enum Op
        {
            Greater, GreaterEqual,
            Less, LessEqual,
            Equal
        }

        public class Converter : IValueConverter
        {
            private readonly Op op;

            public Converter(Op op)
            {
                this.op = op;
            }

            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                var d = System.Convert.ToDecimal(value);
                var a = System.Convert.ToDecimal(parameter);
                const decimal eps = 0.0000001m;
                return op switch
                {
                    Op.Greater => d > a,
                    Op.GreaterEqual => d >= a - eps,
                    Op.Less => d < a,
                    Op.LessEqual => d <= a + eps,
                    Op.Equal => Math.Abs(d - a) < eps,
                    _ => throw new NotImplementedException()
                };
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public static readonly Converter Greater = new Converter(Op.Greater);
        public static readonly Converter GreaterEqual = new Converter(Op.GreaterEqual);
        public static readonly Converter Less = new Converter(Op.Less);
        public static readonly Converter LessEqual = new Converter(Op.LessEqual);
        public static readonly Converter Equal = new Converter(Op.Equal);
    }
}
