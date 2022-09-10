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

using System;
using System.Configuration;
using System.IO;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class AppConfigManager : IAppConfigManager
    {
        public bool SaveOnChange { get; set; } = true;

        public string Path { get; private set; } = null!;


        private Configuration defaultConfig = null!;
        private Configuration userConfig = null!;

        public AppConfigManager()
        {
            Load();
        }

        public void Load()
        {
            defaultConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

            if (path.Length == 0)
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            }

            Path = path;

            var mapping = new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            };

            userConfig = ConfigurationManager.OpenMappedExeConfiguration(mapping, ConfigurationUserLevel.None);

            if (!File.Exists(path) && userConfig.AppSettings.Settings.Count == 0)
            {
                foreach (var setting in defaultConfig.AppSettings.Settings)
                {
                    userConfig.AppSettings.Settings.Add(setting as KeyValueConfigurationElement);
                }
                userConfig.Save(ConfigurationSaveMode.Full);
            }

            _valuesReloaded?.Invoke(this, new IAppConfigManager.ValuesReloadedEventArgs());
        }

        public void Save()
        {
            userConfig.Save(ConfigurationSaveMode.Modified);
        }

        public void Set(string key, string? value)
        {
            var current = userConfig.AppSettings.Settings[key];

            if (current != null)
            {
                userConfig.AppSettings.Settings.Remove(key);
            }

            if (value != null)
            {
                userConfig.AppSettings.Settings.Add(key, value);
            }

            if (current?.Value != value)
            {
                if (SaveOnChange)
                {
                    Save();
                }

                _valueChanged?.Invoke(this, new IAppConfigManager.ValueChangedEventArgs(key));
            }
        }

        public string? Get(string key, string? fallback = null)
        {
            var entry = userConfig.AppSettings.Settings[key];

            if (entry != null)
            {
                return entry.Value;
            }

            // Fall back to default config and then provided fallback value
            return defaultConfig.AppSettings.Settings[key]?.Value ?? fallback;
        }

        public bool Contains(string key)
        {
            return userConfig.AppSettings.Settings[key]?.Value != null;
        }

        private EventHandler<IAppConfigManager.ValueChangedEventArgs>? _valueChanged;

        public event EventHandler<IAppConfigManager.ValueChangedEventArgs> ValueChanged
        {
            add => _valueChanged += value;
            remove => _valueChanged -= value;
        }

        private EventHandler<IAppConfigManager.ValuesReloadedEventArgs>? _valuesReloaded;

        public event EventHandler<IAppConfigManager.ValuesReloadedEventArgs> ValuesReloaded
        {
            add => _valuesReloaded += value;
            remove => _valuesReloaded -= value;
        }
    }
}
