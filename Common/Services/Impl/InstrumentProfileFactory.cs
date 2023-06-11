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
using FitsRatingTool.Common.Models.Instrument;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

namespace FitsRatingTool.Common.Services.Impl
{
    public class InstrumentProfileFactory : IInstrumentProfileFactory
    {
        private class InstrumentProfile : IInstrumentProfile
        {
            private class JsonVariable
            {
                [JsonProperty(PropertyName = "type", Required = Required.Always)]
                [JsonConverter(typeof(StringEnumConverter))]
                public VariableType Type { get; set; }

                [JsonProperty(PropertyName = "name", Required = Required.Always)]
                public string Name { get; set; } = null!;

                [JsonProperty(PropertyName = "default_value", Required = Required.Always)]
                public double DefaultValue { get; set; }

                [JsonProperty(PropertyName = "keyword", NullValueHandling = NullValueHandling.Ignore)]
                public string Keyword { get; set; } = "";

                [JsonProperty(PropertyName = "exclude_from_aggregate_functions_if_not_found", NullValueHandling = NullValueHandling.Ignore)]
                public bool ExcludeFromAggregateFunctionsIfNotFound { get; set; } = false;

                public IVariable Build()
                {
                    switch (Type)
                    {
                        default:
                        case VariableType.Constant:
                            return new ConstantVariable(Name)
                            {
                                DefaultValue = DefaultValue
                            };
                        case VariableType.Keyword:
                            return new KeywordVariable(Name, Keyword)
                            {
                                DefaultValue = DefaultValue,
                                ExcludeFromAggregateFunctionsIfNotFound = ExcludeFromAggregateFunctionsIfNotFound
                            };
                    }
                }
            }


            [JsonProperty(PropertyName = "id", Required = Required.Always)]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "name", Required = Required.Always)]
            public string Name { get; set; } = "";

            [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
            public string Description { get; set; } = "";

            [JsonProperty(PropertyName = "key", NullValueHandling = NullValueHandling.Ignore)]
            public string Key { get; set; } = "";

            [JsonProperty(PropertyName = "focal_length", NullValueHandling = NullValueHandling.Ignore)]
            public float? FocalLength { get; set; }

            [JsonProperty(PropertyName = "bit_depth", NullValueHandling = NullValueHandling.Ignore)]
            public int? BitDepth { get; set; }

            [JsonProperty(PropertyName = "e_per_adu", NullValueHandling = NullValueHandling.Ignore)]
            public float? ElectronsPerADU { get; set; }

            [JsonProperty(PropertyName = "pixel_size", NullValueHandling = NullValueHandling.Ignore)]
            public float? PixelSizeInMicrons { get; set; }

            [JsonProperty(PropertyName = "variables", NullValueHandling = NullValueHandling.Ignore)]
            private JsonVariable[]? _serializedVariables;
            [JsonIgnore]
            private IVariable[]? _cachedVariables;
            [JsonIgnore]
            public IReadOnlyList<IVariable> Variables
            {
                get
                {
                    if (_cachedVariables != null)
                    {
                        return _cachedVariables;
                    }
                    _cachedVariables = null;
                    if (_serializedVariables != null)
                    {
                        _cachedVariables = new IVariable[_serializedVariables.Length];
                        int i = 0;
                        foreach (var constant in _serializedVariables)
                        {
                            _cachedVariables[i++] = constant.Build();
                        }
                    }
                    _cachedVariables ??= Array.Empty<IVariable>();
                    return _cachedVariables;
                }
                set
                {
                    _cachedVariables = null;
                    _serializedVariables = null;
                    if (value != null)
                    {
                        _serializedVariables = new JsonVariable[value.Count];
                        int i = 0;
                        foreach (var constant in value)
                        {
                            var jsonVar = new JsonVariable();
                            // TODO 
                            if (constant is IKeywordVariable kwVar)
                            {
                                jsonVar.Type = VariableType.Keyword;
                                jsonVar.Keyword = kwVar.Keyword;
                                jsonVar.ExcludeFromAggregateFunctionsIfNotFound = kwVar.ExcludeFromAggregateFunctionsIfNotFound;
                            }
                            jsonVar.Name = constant.Name;
                            jsonVar.DefaultValue = constant.DefaultValue;
                            _serializedVariables[i++] = jsonVar;
                        }
                    }
                }
            }

            [JsonIgnore]
            IReadOnlyList<IReadOnlyVariable> IReadOnlyInstrumentProfile.Variables => Variables;

            public InstrumentProfile(string profileId)
            {
                Id = profileId;
            }
        }

        private class InstrumentProfileBuilder : IInstrumentProfileFactory.IBuilder
        {
            private string? profileId;

            public IInstrumentProfileFactory.IBuilder Id(string profileId)
            {
                this.profileId = profileId;
                return this;
            }

            public IInstrumentProfile Build()
            {
                if (profileId == null)
                {
                    throw new InvalidOperationException("Profile ID must not be null");
                }
                return new InstrumentProfile(profileId);
            }
        }

        public IInstrumentProfileFactory.IBuilder Builder()
        {
            return new InstrumentProfileBuilder();
        }

        public IInstrumentProfile Load(string data)
        {
            List<string> errors = new();

            var profile = JsonConvert.DeserializeObject<InstrumentProfile>(data,
                new JsonSerializerSettings
                {
                    Error = delegate (object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        errors.Add(args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    }
                });

            if (profile == null || errors.Count > 0)
            {
                StringBuilder str = new();

                str.Append("Invalid instrument profile: ");

                foreach (var error in errors)
                {
                    str.AppendLine(error);
                }

                throw new IInstrumentProfileFactory.InvalidInstrumentProfileException(str.ToString(), null);
            }

            bool isIdValid = profile.Id.Length > 0 && profile.Id.All(x => char.IsLetterOrDigit(x) || x == '_');
            if (!isIdValid)
            {
                throw new IInstrumentProfileFactory.InvalidInstrumentProfileException("Invalid ID: " + profile.Id, null);
            }

            foreach (var constant in profile.Variables)
            {
                bool isNameValid = constant.Name.Length > 0 && char.IsLetter(constant.Name[0]) && constant.Name.All(x => char.IsLetterOrDigit(x));
                if (!isNameValid)
                {
                    throw new IInstrumentProfileFactory.InvalidInstrumentProfileException("Invalid constant name: " + constant.Name, null);
                }
            }

            return profile;
        }

        public string Save(IReadOnlyInstrumentProfile profile)
        {
            if (profile is InstrumentProfile instrumentProfile)
            {
                return JsonConvert.SerializeObject(profile, Formatting.Indented);
            }
            else
            {
                throw new ArgumentException("The specified " + nameof(profile) + " was not created by " + nameof(InstrumentProfileFactory));
            }
        }
    }
}
