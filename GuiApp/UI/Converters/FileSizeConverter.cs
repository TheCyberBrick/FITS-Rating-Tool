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
    public static class FileSizeConverter
    {
        private class Converter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                long bytes = System.Convert.ToInt64(value);

                if (bytes < 1000)
                {
                    return string.Format("{0} B", bytes);
                }
                else if (bytes < 1000000)
                {
                    return string.Format("{0:0.##} KB", bytes / 1000.0);
                }
                else if (bytes < 1000000000)
                {
                    return string.Format("{0:0.##} MB", bytes / 1000000.0);
                }
                else
                {
                    return string.Format("{0:0.##} GB", bytes / 1000000000.0);
                }
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public static readonly IValueConverter StringWithSuffix = new Converter();
    }
}
