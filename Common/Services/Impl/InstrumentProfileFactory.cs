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
using System.Text;

namespace FitsRatingTool.Common.Services.Impl
{
    public class InstrumentProfileFactory : IInstrumentProfileFactory
    {
        private class InstrumentProfile : IInstrumentProfile
        {
            public class JsonVariableConfig
            {
                [JsonProperty(PropertyName = "id", Required = Required.Always)]
                public string Id { get; set; } = null!;

                [JsonProperty(PropertyName = "name", Required = Required.Always)]
                public string Name { get; set; } = null!;

                [JsonProperty(PropertyName = "config", Required = Required.Always)]
                [JsonConverter(typeof(JsonToStringConverter))]
                public string Config { get; set; } = null!;
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


            [JsonProperty(PropertyName = "grouping_keys", NullValueHandling = NullValueHandling.Ignore)]
            public IReadOnlyCollection<string>? GroupingKeys { get; set; }


            [JsonProperty(PropertyName = "variables", NullValueHandling = NullValueHandling.Ignore)]
            private JsonVariableConfig[]? _serializedVariables;
            [JsonIgnore]
            private IReadOnlyJobConfig.VariableConfig[]? _cachedVariables;
            [JsonIgnore]
            public IReadOnlyList<IReadOnlyJobConfig.VariableConfig>? Variables
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
                        _cachedVariables = new IReadOnlyJobConfig.VariableConfig[_serializedVariables.Length];
                        int i = 0;
                        foreach (var cfg in _serializedVariables)
                        {
                            _cachedVariables[i++] = new IReadOnlyJobConfig.VariableConfig(cfg.Id, cfg.Name, cfg.Config);
                        }
                    }
                    return _cachedVariables;
                }
                set
                {
                    _cachedVariables = null;
                    _serializedVariables = null;
                    if (value != null)
                    {
                        _serializedVariables = new JsonVariableConfig[value.Count];
                        int i = 0;
                        foreach (var cfg in value)
                        {
                            _serializedVariables[i++] = new JsonVariableConfig
                            {
                                Id = cfg.Id,
                                Name = cfg.Name,
                                Config = cfg.Config
                            };
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


        private readonly IVariableManager variableManager;

        public InstrumentProfileFactory(IVariableManager variableManager)
        {
            this.variableManager = variableManager;
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

            if (profile.Variables != null)
            {
                foreach (var cfg in profile.Variables)
                {
                    if (!variableManager.TryCreateVariable(cfg.Id, cfg.Name, cfg.Config, out var _))
                    {
                        throw new IInstrumentProfileFactory.InvalidInstrumentProfileException("Invalid or unknown variable: " + cfg.Id, null);
                    }
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
