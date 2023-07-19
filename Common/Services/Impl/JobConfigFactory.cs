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
using Newtonsoft.Json.Converters;
using System.Text;

namespace FitsRatingTool.Common.Services.Impl
{
    public class JobConfigFactory : IJobConfigFactory
    {
        private class JobConfig : IJobConfig
        {
            public class JsonGroupingFilterConfig
            {
                [JsonProperty(PropertyName = "grouping_key", Required = Required.Always)]
                public string GroupingKey { get; set; } = null!;

                [JsonProperty(PropertyName = "pattern", Required = Required.Always)]
                public string Pattern { get; set; } = null!;
            }

            public class JsonExporterConfig
            {
                [JsonProperty(PropertyName = "id", Required = Required.Always)]
                public string Id { get; set; } = null!;

                [JsonProperty(PropertyName = "config", Required = Required.Always)]
                [JsonConverter(typeof(JsonToStringConverter))]
                public string Config { get; set; } = null!;
            }

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



            [JsonIgnore]
            private string[] _evaluationFormulaLines = default!;
            [JsonProperty(PropertyName = "evaluation_formula", Required = Required.Always)]
            public string[] EvaluationFormulaLines
            {
                get => _evaluationFormulaLines;
                set
                {
                    _evaluationFormulaLines = value;
                    cachedFormula = null;
                }
            }

            [JsonIgnore]
            private string? cachedFormula = null;
            [JsonIgnore]
            public string EvaluationFormula
            {
                get
                {
                    if (cachedFormula != null)
                    {
                        return cachedFormula;
                    }
                    StringBuilder str = new();
                    foreach (var line in EvaluationFormulaLines)
                    {
                        str.AppendLine(line);
                    }
                    return cachedFormula = str.ToString();
                }
                set
                {
                    EvaluationFormulaLines = value.Split(Environment.NewLine);
                }
            }

            [JsonProperty(PropertyName = "parallel_tasks")]
            public int ParallelTasks { get; set; } = 4;

            [JsonProperty(PropertyName = "parallel_io")]
            public int ParallelIO { get; set; } = 1;

            [JsonProperty(PropertyName = "grouping_keys", NullValueHandling = NullValueHandling.Ignore)]
            public IReadOnlyCollection<string>? GroupingKeys { get; set; }

            [JsonProperty(PropertyName = "grouping_keys_required", NullValueHandling = NullValueHandling.Ignore)]
            public bool GroupingKeysRequired { get; set; } = true;

            [JsonProperty(PropertyName = "grouping_filters", NullValueHandling = NullValueHandling.Ignore)]
            private JsonGroupingFilterConfig[]? _serializedGroupingFilters;
            [JsonIgnore]
            private IReadOnlyJobConfig.FilterConfig[]? _cachedGroupingFilters;
            [JsonIgnore]
            public IReadOnlyCollection<IReadOnlyJobConfig.FilterConfig>? GroupingFilters
            {
                get
                {
                    if (_cachedGroupingFilters != null)
                    {
                        return _cachedGroupingFilters;
                    }
                    _cachedGroupingFilters = null;
                    if (_serializedGroupingFilters != null)
                    {
                        _cachedGroupingFilters = new IReadOnlyJobConfig.FilterConfig[_serializedGroupingFilters.Length];
                        int i = 0;
                        foreach (var cfg in _serializedGroupingFilters)
                        {
                            _cachedGroupingFilters[i++] = new IReadOnlyJobConfig.FilterConfig(cfg.GroupingKey, cfg.Pattern);
                        }
                    }
                    return _cachedGroupingFilters;
                }
                set
                {
                    _cachedGroupingFilters = null;
                    _serializedGroupingFilters = null;
                    if (value != null)
                    {
                        _serializedGroupingFilters = new JsonGroupingFilterConfig[value.Count];
                        int i = 0;
                        foreach (var cfg in value)
                        {
                            _serializedGroupingFilters[i++] = new JsonGroupingFilterConfig
                            {
                                GroupingKey = cfg.Key,
                                Pattern = cfg.Pattern
                            };
                        }
                    }
                }
            }

            [JsonProperty(PropertyName = "output_logs_path", NullValueHandling = NullValueHandling.Ignore)]
            public string? OutputLogsPath { get; set; }


            [JsonProperty(PropertyName = "cache_path", NullValueHandling = NullValueHandling.Ignore)]
            public string? CachePath { get; set; }


            [JsonProperty(PropertyName = "max_image_size", NullValueHandling = NullValueHandling.Ignore)]
            public long MaxImageSize { get; set; } = 805306368;

            [JsonProperty(PropertyName = "max_image_width", NullValueHandling = NullValueHandling.Ignore)]
            public int MaxImageWidth { get; set; } = 8192;

            [JsonProperty(PropertyName = "max_image_height", NullValueHandling = NullValueHandling.Ignore)]
            public int MaxImageHeight { get; set; } = 8192;


            [JsonProperty(PropertyName = "exporters", NullValueHandling = NullValueHandling.Ignore)]
            private JsonExporterConfig[]? _serializedExporters;
            [JsonIgnore]
            private IReadOnlyJobConfig.ExporterConfig[]? _cachedExporters;
            [JsonIgnore]
            public IReadOnlyCollection<IReadOnlyJobConfig.ExporterConfig>? Exporters
            {
                get
                {
                    if (_cachedExporters != null)
                    {
                        return _cachedExporters;
                    }
                    _cachedExporters = null;
                    if (_serializedExporters != null)
                    {
                        _cachedExporters = new IReadOnlyJobConfig.ExporterConfig[_serializedExporters.Length];
                        int i = 0;
                        foreach (var cfg in _serializedExporters)
                        {
                            _cachedExporters[i++] = new IReadOnlyJobConfig.ExporterConfig(cfg.Id, cfg.Config);
                        }
                    }
                    return _cachedExporters;
                }
                set
                {
                    _cachedExporters = null;
                    _serializedExporters = null;
                    if (value != null)
                    {
                        _serializedExporters = new JsonExporterConfig[value.Count];
                        int i = 0;
                        foreach (var cfg in value)
                        {
                            _serializedExporters[i++] = new JsonExporterConfig
                            {
                                Id = cfg.Id,
                                Config = cfg.Config
                            };
                        }
                    }
                }
            }

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
        }

        private class JobConfigBuilder : IConfigBuilder<IJobConfig>
        {
            public IJobConfig Build()
            {
                return new JobConfig();
            }
        }

        public IConfigBuilder<IJobConfig> Builder()
        {
            return new JobConfigBuilder();
        }

        public IJobConfig Load(string data)
        {
            List<string> errors = new();

            var config = JsonConvert.DeserializeObject<JobConfig>(data,
                new JsonSerializerSettings
                {
                    Error = delegate (object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        errors.Add(args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    }
                });

            if (config == null || errors.Count > 0)
            {
                StringBuilder str = new();

                str.Append("Invalid job config: ");

                foreach (var error in errors)
                {
                    str.AppendLine(error);
                }

                throw new IJobConfigFactory.InvalidJobConfigException(str.ToString(), null);
            }

            return config;
        }

        public string Save(IReadOnlyJobConfig config)
        {
            if (config is JobConfig jobConfig)
            {
                return JsonConvert.SerializeObject(config, Formatting.Indented);
            }
            else
            {
                throw new ArgumentException("The specified " + nameof(config) + " was not created by " + nameof(JobConfigFactory));
            }
        }
    }
}
