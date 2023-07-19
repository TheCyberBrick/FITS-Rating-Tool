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
using FitsRatingTool.Common.Models.Evaluation;
using Newtonsoft.Json;

namespace FitsRatingTool.Variables.Services.Impl
{
    public class KeywordVariableFactory : IKeywordVariableFactory
    {
        public class Config
        {
            [JsonProperty(PropertyName = "keyword", Required = Required.Always)]
            public string Keyword { get; set; } = null!;

            [JsonProperty(PropertyName = "default_value", NullValueHandling = NullValueHandling.Ignore)]
            public double DefaultValue { get; set; } = 0.0;

            [JsonProperty(PropertyName = "exclude_from_aggregate_functions_if_not_found", NullValueHandling = NullValueHandling.Ignore)]
            public bool ExcludeFromAggregateFunctionsIfNotFound { get; set; } = true;
        }

        public class Variable : IVariable
        {
            public string Name { get; set; }

            public double DefaultValue { get; set; }

            public string Keyword { get; set; }

            public bool ExcludeFromAggregateFunctionsIfNotFound { get; set; }

            public Variable(string name, Config config)
            {
                Name = name;
                DefaultValue = config.DefaultValue;
                Keyword = config.Keyword;
                ExcludeFromAggregateFunctionsIfNotFound = config.ExcludeFromAggregateFunctionsIfNotFound;
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

        public string Description => "A variable that resolves to the value of a FITS header keyword.";

        public string ExampleConfig => JsonConvert.SerializeObject(new Config()
        {
            Keyword = "MyFITSKeyword",
            DefaultValue = 0.0,
            ExcludeFromAggregateFunctionsIfNotFound = true
        }, Formatting.Indented);

        public IVariable Create(string name, string config)
        {
            throw new NotImplementedException();
        }
    }
}
