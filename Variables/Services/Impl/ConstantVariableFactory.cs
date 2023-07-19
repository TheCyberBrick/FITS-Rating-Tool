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

using FitsRatingTool.Common.Models.Evaluation;
using Newtonsoft.Json;

namespace FitsRatingTool.Variables.Services.Impl
{
    public class ConstantVariableFactory : IConstantVariableFactory
    {
        public class Config
        {
            [JsonProperty(PropertyName = "value", Required = Required.Always)]
            public double Value { get; set; }
        }

        public class Variable : IVariable
        {
            public string Name { get; set; }

            public double DefaultValue { get; set; }

            public Variable(string name, Config config)
            {
                Name = name;
                DefaultValue = config.Value;
            }

            public Task<Constant> EvaluateAsync(string file, Func<string, string?> header)
            {
                return Task.FromResult(new Constant(Name, DefaultValue, false));
            }
        }

        public string Description => "A variable that resolves to a constant value.";

        public string ExampleConfig => JsonConvert.SerializeObject(new Config()
        {
            Value = 123.0
        }, Formatting.Indented);

        public IVariable Create(string name, string config)
        {
            var cfg = JsonConvert.DeserializeObject<Config>(config);
            if (cfg == null)
            {
                throw new ArgumentException("Invalid config");
            }
            return new Variable(name, cfg);
        }
    }
}
