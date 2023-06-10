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
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FitsRatingTool.Common.Services.Impl
{
    public class InstrumentProfileFactory : IInstrumentProfileFactory
    {
        private class InstrumentProfile : IInstrumentProfile
        {
            private class JsonConstant : IConstant
            {
                [JsonProperty(PropertyName = "name", Required = Required.Always)]
                public string Name { get; set; } = null!;

                [JsonProperty(PropertyName = "value", Required = Required.Always)]
                public double Value { get; set; }
            }

            public class JsonValueOverrideSpecification
            {
                [JsonProperty(PropertyName = "keyword", Required = Required.Always)]
                public string Keyword { get; set; } = null!;

                [JsonProperty(PropertyName = "default_value", Required = Required.Always)]
                public double DefaultValue { get; set; } = 0;

                [JsonProperty(PropertyName = "exclude_from_aggregate_functions_if_not_found", NullValueHandling = NullValueHandling.Ignore)]
                public bool ExcludeFromAggregateFunctionsIfNotFound { get; set; } = false;
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

            [JsonProperty(PropertyName = "constants", NullValueHandling = NullValueHandling.Ignore)]
            private JsonConstant[]? _serializedConstants;
            [JsonIgnore]
            private IConstant[]? _cachedConstants;
            [JsonIgnore]
            public IReadOnlyList<IConstant> Constants
            {
                get
                {
                    if (_cachedConstants != null)
                    {
                        return _cachedConstants;
                    }
                    _cachedConstants = null;
                    if (_serializedConstants != null)
                    {
                        _cachedConstants = new IInstrumentProfile.Constant[_serializedConstants.Length];
                        int i = 0;
                        foreach (var constant in _serializedConstants)
                        {
                            _cachedConstants[i++] = new IInstrumentProfile.Constant()
                            {
                                Name = constant.Name,
                                Value = constant.Value
                            };
                        }
                    }
                    _cachedConstants ??= Array.Empty<IInstrumentProfile.Constant>();
                    return _cachedConstants;
                }
                set
                {
                    _cachedConstants = null;
                    _serializedConstants = null;
                    if (value != null)
                    {
                        _serializedConstants = new JsonConstant[value.Count];
                        int i = 0;
                        foreach (var constant in value)
                        {
                            _serializedConstants[i++] = new JsonConstant
                            {
                                Name = constant.Name,
                                Value = constant.Value
                            };
                        }
                    }
                }
            }

            [JsonIgnore]
            IReadOnlyList<IReadOnlyConstant> IReadOnlyInstrumentProfile.Constants => Constants;

            [JsonProperty(PropertyName = "value_overrides", NullValueHandling = NullValueHandling.Ignore)]
            private Dictionary<string, JsonValueOverrideSpecification>? _serializedValueOverrides;
            [JsonIgnore]
            private Dictionary<string, ValueOverrideSpecification>? _cachedValueOverrides;
            [JsonIgnore]
            public IReadOnlyDictionary<string, ValueOverrideSpecification>? ValueOverrides
            {
                get
                {
                    if (_cachedValueOverrides != null)
                    {
                        return _cachedValueOverrides;
                    }
                    _cachedValueOverrides = null;
                    if (_serializedValueOverrides != null)
                    {
                        _cachedValueOverrides = new();
                        foreach (var entry in _serializedValueOverrides)
                        {
                            _cachedValueOverrides[entry.Key] = new ValueOverrideSpecification
                            {
                                Keyword = entry.Value.Keyword,
                                DefaultValue = entry.Value.DefaultValue,
                                ExcludeFromAggregateFunctionsIfNotFound = entry.Value.ExcludeFromAggregateFunctionsIfNotFound
                            };
                        }
                    }
                    return _cachedValueOverrides;
                }
                set
                {
                    _cachedValueOverrides = null;
                    _serializedValueOverrides = null;
                    if (value != null)
                    {
                        _serializedValueOverrides = new();
                        foreach (var entry in value)
                        {
                            _serializedValueOverrides.Add(entry.Key, new JsonValueOverrideSpecification
                            {
                                Keyword = entry.Value.Keyword,
                                DefaultValue = entry.Value.DefaultValue,
                                ExcludeFromAggregateFunctionsIfNotFound = entry.Value.ExcludeFromAggregateFunctionsIfNotFound
                            });
                        }
                    }
                }
            }

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

            foreach (var constant in profile.Constants)
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
