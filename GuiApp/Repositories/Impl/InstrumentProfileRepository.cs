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

using FitsRatingTool.Common.Models.Instrument;
using FitsRatingTool.Common.Services;
using FitsRatingTool.GuiApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FitsRatingTool.GuiApp.Repositories.Impl
{
    public class InstrumentProfileRepository : IInstrumentProfileRepository
    {
        private Dictionary<string, IReadOnlyInstrumentProfile> profiles = new Dictionary<string, IReadOnlyInstrumentProfile>();

        public IReadOnlyCollection<string> ProfileIds => profiles.Keys;


        private string ProfilesPath => Path.Combine(Directory.GetParent(appConfigManager.Path)?.FullName ?? appConfigManager.Path, "profiles");

        private readonly IAppConfigManager appConfigManager;
        private readonly IInstrumentProfileFactory instrumentProfileFactory;

        public InstrumentProfileRepository(IAppConfigManager appConfigManager, IInstrumentProfileFactory instrumentProfileFactory)
        {
            this.appConfigManager = appConfigManager;
            this.instrumentProfileFactory = instrumentProfileFactory;

            Directory.CreateDirectory(ProfilesPath);

            Load();
        }

        private void Load()
        {
            Dictionary<string, IReadOnlyInstrumentProfile> removedProfiles = new Dictionary<string, IReadOnlyInstrumentProfile>(profiles);

            foreach (var file in Directory.EnumerateFiles(ProfilesPath, "*.json"))
            {
                try
                {
                    var profile = instrumentProfileFactory.Load(File.ReadAllText(file, Encoding.UTF8));

                    if (removedProfiles.Remove(profile.Id, out var currentProfile))
                    {
                        profiles[profile.Id] = profile;

                        _profileChanged?.Invoke(this, new IInstrumentProfileRepository.InstrumentProfileEventArgs(profile.Id, true, false, false));
                    }
                    else
                    {
                        profiles.Add(profile.Id, profile);

                        _profileAdded?.Invoke(this, new IInstrumentProfileRepository.InstrumentProfileEventArgs(profile.Id, false, true, false));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            }

            foreach (var removedProfileId in removedProfiles.Keys)
            {
                profiles.Remove(removedProfileId);

                _profileRemoved?.Invoke(this, new IInstrumentProfileRepository.InstrumentProfileEventArgs(removedProfileId, false, false, true));
            }
        }

        public void AddOrUpdateProfile(IReadOnlyInstrumentProfile profile)
        {
            if (profiles.ContainsKey(profile.Id))
            {
                profiles[profile.Id] = profile;

                _profileChanged?.Invoke(this, new IInstrumentProfileRepository.InstrumentProfileEventArgs(profile.Id, true, false, false));
            }
            else
            {
                profiles.Add(profile.Id, profile);

                _profileAdded?.Invoke(this, new IInstrumentProfileRepository.InstrumentProfileEventArgs(profile.Id, false, true, false));
            }
        }

        public IReadOnlyInstrumentProfile? GetProfile(string id)
        {
            return profiles.TryGetValue(id, out var profile) ? profile : null;
        }

        public void RemoveProfile(string id)
        {
            if (profiles.Remove(id))
            {
                _profileRemoved?.Invoke(this, new IInstrumentProfileRepository.InstrumentProfileEventArgs(id, false, false, true));

                File.Delete(GetProfilePath(id));
            }
        }

        public void Save(string id)
        {
            var profile = GetProfile(id);

            if (profile != null)
            {
                var data = instrumentProfileFactory.Save(profile);

                File.WriteAllText(GetProfilePath(profile.Id), data, Encoding.UTF8);
            }
        }

        public void SaveAll()
        {
            foreach (var profileId in profiles.Keys)
            {
                Save(profileId);
            }
        }

        private string GetProfilePath(string profileId)
        {
            return Path.Combine(ProfilesPath, profileId + ".json");
        }

        private EventHandler<IInstrumentProfileRepository.InstrumentProfileEventArgs>? _profileChanged;
        public event EventHandler<IInstrumentProfileRepository.InstrumentProfileEventArgs> ProfileChanged
        {
            add => _profileChanged += value;
            remove => _profileChanged -= value;
        }

        private EventHandler<IInstrumentProfileRepository.InstrumentProfileEventArgs>? _profileAdded;
        public event EventHandler<IInstrumentProfileRepository.InstrumentProfileEventArgs> ProfileAdded
        {
            add => _profileAdded += value;
            remove => _profileAdded -= value;
        }

        private EventHandler<IInstrumentProfileRepository.InstrumentProfileEventArgs>? _profileRemoved;
        public event EventHandler<IInstrumentProfileRepository.InstrumentProfileEventArgs> ProfileRemoved
        {
            add => _profileRemoved += value;
            remove => _profileRemoved -= value;
        }
    }
}
