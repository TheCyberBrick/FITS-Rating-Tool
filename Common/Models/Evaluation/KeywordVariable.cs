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

using System.Globalization;

namespace FitsRatingTool.Common.Models.Evaluation
{
    public class KeywordVariable : IKeywordVariable
    {
        public string Name { get; set; }

        public double DefaultValue { get; set; }

        public string Keyword { get; set; }

        public bool ExcludeFromAggregateFunctionsIfNotFound { get; set; }

        public KeywordVariable(string name, string keyword)
        {
            Name = name;
            Keyword = keyword;
        }

        public Task<Constant> EvaluateAsync(string file, Func<string, string?> header)
        {
            double? value = null;
            if (double.TryParse(header.Invoke(Keyword), NumberStyles.Float, CultureInfo.InvariantCulture, out double keywordValue))
            {
                value = keywordValue;
            }
            return Task.FromResult(new Constant(Name, value.HasValue ? value.Value : DefaultValue, !value.HasValue && ExcludeFromAggregateFunctionsIfNotFound));
        }
    }
}
