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

using FitsRatingTool.Common.Models;
using System.Text;
using System;
using Newtonsoft.Json;
using static FitsRatingTool.Common.Models.FitsImage.IFitsImageStatistics;

namespace FitsRatingTool.Common.Services.Impl
{
    public partial class StandaloneEvaluationService
    {
        private class Cache : IBatchEvaluationService.ICache
        {
            private class GroupingKeyData
            {
                [JsonProperty(PropertyName = "key", Required = Required.Always)]
                public string Key { get; set; } = null!;

                [JsonProperty(PropertyName = "value", NullValueHandling = NullValueHandling.Ignore)]
                public string? Value { get; set; }
            }

            private class CacheData
            {
                [JsonProperty(PropertyName = "measurements", Required = Required.Always)]
                public Dictionary<string, double> Measurements { get; set; } = null!;

                [JsonProperty(PropertyName = "grouping_keys", Required = Required.Always)]
                public List<GroupingKeyData> GroupingKeys { get; set; } = null!;

                [JsonProperty(PropertyName = "group_key", NullValueHandling = NullValueHandling.Ignore)]
                public string? GroupKey { get; set; } = null!;

                [JsonProperty(PropertyName = "header", Required = Required.Always)]
                public Dictionary<string, string> Header { get; set; } = null!;
            }

            public string CachePath { get; }

            public Cache(string cachePath)
            {
                CachePath = cachePath;
            }

            private string GetCacheFile(string file)
            {
                var root = Path.GetPathRoot(file);
                if (root != null)
                {
                    int rootLen = root.Length;
                    root = root.TrimEnd(Path.DirectorySeparatorChar);
                    StringBuilder rootPrefix = new(root);
                    foreach (var c in Path.GetInvalidFileNameChars())
                    {
                        rootPrefix.Replace(c.ToString(), string.Empty);
                    }
                    rootPrefix.Append(Path.DirectorySeparatorChar);
                    file = string.Concat(rootPrefix.ToString(), file.AsSpan(rootLen, file.Length - rootLen));
                }
                file = string.Concat(CachePath, Path.DirectorySeparatorChar, file.TrimStart(Path.DirectorySeparatorChar), ".srscache");
                return file;
            }

            public void Save(string file, IEnumerable<KeyValuePair<MeasurementType, double>> stats, IEnumerable<KeyValuePair<string, string?>> groupingKeys, string? groupKey, IEnumerable<KeyValuePair<string, string>> header)
            {
                var cacheFile = GetCacheFile(file);

                var parentPath = Directory.GetParent(cacheFile);
                if (parentPath != null)
                {
                    Directory.CreateDirectory(parentPath.FullName);
                }

                List<GroupingKeyData> groupingKeysList = new();
                foreach (var pair in groupingKeys)
                {
                    groupingKeysList.Add(new GroupingKeyData()
                    {
                        Key = pair.Key,
                        Value = pair.Value
                    });
                }

                var data = new CacheData
                {
                    Measurements = new(),
                    GroupingKeys = groupingKeysList,
                    GroupKey = groupKey,
                    Header = new(header)
                };

                foreach (var stat in stats)
                {
                    data.Measurements.Add(Enum.GetName(typeof(MeasurementType), stat.Key)!, stat.Value);
                }

                File.WriteAllText(cacheFile, JsonConvert.SerializeObject(data));
            }

            public bool Load(string file, out IDictionary<MeasurementType, double>? stats, out IDictionary<string, string?>? groupingKeys, out string? groupKey, out IDictionary<string, string>? header)
            {
                stats = null;
                groupingKeys = null;
                groupKey = null;
                header = null;

                var cacheFile = GetCacheFile(file);
                if (!File.Exists(cacheFile))
                {
                    return false;
                }

                var data = JsonConvert.DeserializeObject<CacheData>(File.ReadAllText(cacheFile))!;

                if (data == null)
                {
                    throw new Exception("Cached data is empty or invalid");
                }

                stats = new Dictionary<MeasurementType, double>();
                groupingKeys = new Dictionary<string, string?>();
                groupKey = data.GroupKey;
                header = data.Header;

                foreach (var key in data.GroupingKeys)
                {
                    groupingKeys.Add(key.Key, key.Value);
                }

                foreach (var stat in data.Measurements)
                {
                    if (Enum.TryParse<MeasurementType>(stat.Key, out var measurement))
                    {
                        stats.Add(measurement, stat.Value);
                    }
                    else
                    {
                        throw new Exception("Unknown measurement '" + stat.Key + "'");
                    }
                }

                return true;
            }

            public int Delete(IEnumerable<string> files)
            {
                int n = 0;
                foreach (var file in files)
                {
                    var cacheFile = GetCacheFile(file);
                    if (File.Exists(cacheFile))
                    {
                        File.Delete(cacheFile);
                        ++n;
                    }
                }
                return n;
            }

            public int Clear()
            {
                int n = 0;
                if (Directory.Exists(CachePath))
                {
                    foreach (string file in Directory.EnumerateFiles(CachePath, "*.srscache", SearchOption.AllDirectories))
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            ++n;
                        }
                    }
                }
                return n;
            }
        }
    }
}
