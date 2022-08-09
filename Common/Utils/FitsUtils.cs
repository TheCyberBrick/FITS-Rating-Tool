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

namespace FitsRatingTool.Common.Utils
{
    public static class FitsUtils
    {
        private static readonly string[] FILTER_NUMBER_KEYWORDS = { "FILTNUM", "FILTNUMBER", "FILTNR", "FILTN", "FILTERNUM", "FILTERNUMBER", "FILTERNR", "FILTERN" };
        private static readonly string[] FILTER_KEYWORDS = { "FILT0", "FILT-0", "FILTER0", "FILTER-0", "FILT1", "FILT-1", "FILTER1", "FILTER-1", "FILT2", "FILT-2", "FILTER2", "FILTER-2", "FILTER", "FILT" };

        public static string? FindFilter(Func<string, string?> header, bool allowMultiple = false)
        {
            bool tryGetHeaderValue(string keyword, out string? value)
            {
                value = header.Invoke(keyword);
                return value != null;
            }

            if (tryGetHeaderValue("FILTER", out var filter))
            {
                return filter;
            }

            int filterCount = -1;
            if (!allowMultiple)
            {
                foreach (var filterNumberKeyword in FILTER_NUMBER_KEYWORDS)
                {
                    if (tryGetHeaderValue(filterNumberKeyword, out var val) && int.TryParse(val, out var nr))
                    {
                        filterCount = nr;
                        break;
                    }
                }
            }
            if (filterCount == -1)
            {
                filterCount = 1;
            }

            if (filterCount == 1)
            {
                foreach (var filterKeyword in FILTER_KEYWORDS)
                {
                    if (tryGetHeaderValue(filterKeyword, out filter))
                    {
                        return filter;
                    }
                }
            }

            return null;
        }

        public static DateTime? ParseDate(string dateFitStr)
        {
            var oldFormat = dateFitStr.Split("/");
            if (oldFormat.Length == 3)
            {
                return ParseOldDateFormat(oldFormat);
            }

            var newExtendedFormat = dateFitStr.Split("T");
            if (newExtendedFormat.Length == 2)
            {
                return ParseNewExtendedDateFormat(newExtendedFormat);
            }

            var newFormat = dateFitStr.Split("-");
            if (newFormat.Length == 3)
            {
                return ParseNewDateFormat(newFormat);
            }

            return null;
        }

        private static DateTime? ParseOldDateFormat(string[] data)
        {
            if (!int.TryParse(data[2], out var year) || year < 0 || year > 99)
            {
                return null;
            }
            if (!int.TryParse(data[1], out var month) || month <= 0 || month > 12)
            {
                return null;
            }
            year += 1900;
            if (!int.TryParse(data[0], out var day) || day <= 0 || day > DateTime.DaysInMonth(year, month))
            {
                return null;
            }
            return new DateTime(year, month, day);
        }

        private static DateTime? ParseNewDateFormat(string[] data)
        {
            if (!int.TryParse(data[0], out var year))
            {
                return null;
            }
            if (!int.TryParse(data[1], out var month) || month <= 0 || month > 12)
            {
                return null;
            }
            if (!int.TryParse(data[2], out var day) || day <= 0 || day > DateTime.DaysInMonth(year, month))
            {
                return null;
            }
            return new DateTime(year, month, day);
        }

        private static DateTime? ParseNewExtendedDateFormat(string[] data)
        {
            var newFormat = data[0].Split("-");
            if (newFormat.Length == 3)
            {
                var timeFormat = data[1].Split(":");
                if (timeFormat.Length == 3)
                {
                    var newDate = ParseNewDateFormat(newFormat);
                    DateTime date;
                    if (!newDate.HasValue)
                    {
                        return null;
                    }
                    date = newDate.Value;

                    if (!int.TryParse(timeFormat[0], out var hour) || hour < 0 || hour > 23)
                    {
                        return null;
                    }
                    if (!int.TryParse(timeFormat[1], out var minute) || minute < 0 || minute > 59)
                    {
                        return null;
                    }
                    if (!float.TryParse(timeFormat[2], out var second) || second < 0 || second > 60)
                    {
                        return null;
                    }

                    int wholeSecond = (int)Math.Floor(second);
                    int milliseconds = (int)Math.Floor((second - wholeSecond) * 1000);
                    return new DateTime(date.Year, date.Month, date.Day, hour, minute, wholeSecond, milliseconds, DateTimeKind.Utc);
                }
            }
            return null;
        }
    }
}
