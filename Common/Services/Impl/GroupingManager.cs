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

using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.Common.Utils;
using System.Text;

namespace FitsRatingTool.Common.Services.Impl
{
    public class GroupingManager : IGroupingManager
    {
        private class GroupMatch : IGroupingManager.IGroupMatch
        {
            public IReadOnlyDictionary<string, string?> Matches { get; }

            public string GroupKey { get; }

            public GroupMatch(string key, Dictionary<string, string?> matches)
            {
                Matches = matches;
                GroupKey = key;
            }
        }

        private class Grouping : IGroupingManager.IGrouping
        {
            private readonly List<string> groupingKeys;

            public IReadOnlyList<string> Keys => groupingKeys;

            public bool IsEmpty => groupingKeys.Count == 0;

            public bool IsAll => IsEmpty;

            public Grouping(string[] groupingKeys)
            {
                this.groupingKeys = new(groupingKeys);
                this.groupingKeys.Sort();
            }

            public IGroupingManager.IGroupMatch? GetGroupMatch(Func<string, string?> groupingKeys)
            {
                StringBuilder str = new();
                Dictionary<string, string?> matches = new();
                foreach (var key in this.groupingKeys)
                {
                    str.Append(key);
                    str.Append('=');
                    var match = groupingKeys.Invoke(key);
                    str.Append(match ?? "?");
                    str.Append(", ");
                    matches.TryAdd(key, match);
                }
                if (str.Length > 1)
                {
                    return new GroupMatch(str.ToString().Substring(0, str.Length - 2), matches);
                }
                return null;
            }

            public IGroupingManager.IGroupMatch? GetGroupMatch(IFitsImageMetadata metadata)
            {
                Dictionary<string, string> header = new();
                foreach (var entry in metadata.Header)
                {
                    header.Add(entry.Keyword, entry.Value);
                }
                return GetGroupMatch(metadata.File, keyword =>
                {
                    if (header.TryGetValue(keyword, out var value))
                    {
                        return value;
                    }
                    return null;
                });
            }

            public IGroupingManager.IGroupMatch? GetGroupMatch(string file, Func<string, string?> header)
            {
                return GetGroupMatch(key => GetGroupingValue(file, header, key));
            }

            private string? GetGroupingValue(string file, Func<string, string?> header, string key)
            {
                bool tryGetHeaderValue(string keyword, out string? value)
                {
                    value = header.Invoke(keyword);
                    return value != null;
                }

                if (key.StartsWith("Keyword:"))
                {
                    var keyword = key.Substring(8, key.Length - 8);
                    return header.Invoke(keyword);
                }
                else if (key.StartsWith("ParentDirs:"))
                {
                    var countStr = key.Substring(11, key.Length - 11);
                    if (!int.TryParse(countStr, out var count)) count = 1;
                    List<string> dirs = new();
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var parent = Directory.GetParent(i == 0 ? file : dirs.Last());
                            if (parent != null)
                            {
                                dirs.Add(parent.FullName);
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                    if (dirs.Count > 0)
                    {
                        StringBuilder sb = new();
                        for (int i = dirs.Count - 1; i >= 0; --i)
                        {
                            var name = Path.GetFileName(dirs[i]);
                            if (name.Length == 0 && i == dirs.Count - 1)
                            {
                                sb.Append(Path.GetPathRoot(file));
                            }
                            else
                            {
                                sb.Append(name);
                                if (i != 0) sb.Append(Path.DirectorySeparatorChar);
                            }
                        }
                        return sb.ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
                switch (key)
                {
                    case "Object":
                        if (tryGetHeaderValue("OBJECT", out var obj))
                        {
                            return obj;
                        }
                        return null;
                    case "Filter":
                        return FitsUtils.FindFilter(header);
                    case "ExposureTime":
                        if (tryGetHeaderValue("EXPTIME", out var exptime))
                        {
                            return double.TryParse(exptime, out var val) ? ((int)Math.Floor(val)).ToString() : null;
                        }
                        return null;
                    case "Gain":
                        if (tryGetHeaderValue("GAIN", out var gain))
                        {
                            return int.TryParse(gain, out var val) ? val.ToString() : null;
                        }
                        return null;
                    case "Offset":
                        if (tryGetHeaderValue("OFFSET", out var offset))
                        {
                            return int.TryParse(offset, out var val) ? val.ToString() : null;
                        }
                        return null;
                    case "ParentDir":
                        try
                        {
                            var dir = Directory.GetParent(file);
                            return dir != null ? dir.Name : null;
                        }
                        catch (Exception)
                        {
                            return null;
                        }

                }
                return null;
            }
        }

        public IGroupingManager.IGrouping BuildGrouping(params string[] groupingKeys)
        {
            return new Grouping(groupingKeys);
        }

        public bool TryParseGroupingKey(string input, out string key, out object[] args)
        {
            key = "";
            args = Array.Empty<object>();

            var split = input.Split(":");
            var keyStr = split[0];
            var argsStr = split.Length == 2 ? split[1] : null;

            if (keyStr.Equals("Keyword") && argsStr != null)
            {
                key = keyStr;
                args = new string[] { argsStr };
                return true;
            }
            else if (keyStr.Equals("ParentDirs") && argsStr != null && int.TryParse(argsStr, out int parentDirs))
            {
                key = keyStr;
                args = new object[] { parentDirs };
                return true;
            }
            else if ((keyStr.Equals("Object")
                    || keyStr.Equals("Filter")
                    || keyStr.Equals("ExposureTime")
                    || keyStr.Equals("Gain")
                    || keyStr.Equals("Offset")
                    || keyStr.Equals("ParentDir")
                ) && argsStr == null)
            {
                key = keyStr;
                return true;
            }

            return false;
        }
    }
}
