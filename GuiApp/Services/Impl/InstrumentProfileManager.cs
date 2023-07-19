﻿/*
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

using DryIocAttributes;
using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.Common.Models.Instrument;
using FitsRatingTool.Common.Services;
using FitsRatingTool.GuiApp.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    [Export(typeof(IInstrumentProfileManager)), SingletonReuse]
    public class InstrumentProfileManager : IInstrumentProfileManager
    {
        private class Record : IInstrumentProfileManager.IRecord
        {
            public string ProfileId { get; }

            [MaybeNull]
            public IReadOnlyInstrumentProfile Profile
            {
                get => manager.instrumentProfileRepository.GetProfile(ProfileId);
                set
                {
                    if (value != null)
                    {
                        EnsureInRepository();

                        // Create copy through the factory so we can be sure
                        // that it can be saved
                        var copy = manager.CreateProfileFactoryCopy(value);

                        manager.AddOrUpdateProfile(copy);

                        manager.NotifyChange(this, false);
                    }
                    else if (IsValid)
                    {
                        throw new InvalidOperationException("Cannot set '" + nameof(Profile) + "' to null");
                    }
                }
            }

            public bool IsValid => Profile != null;


            private readonly InstrumentProfileManager manager;

            public Record(string profileId, InstrumentProfileManager manager)
            {
                ProfileId = profileId;
                this.manager = manager;
            }

            private void EnsureInRepository()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Profile '" + ProfileId + "' is no longer in the instrument profile repository");
                }
            }
        }

        private readonly ConcurrentDictionary<string, Record> records = new();


        private readonly IInstrumentProfileRepository instrumentProfileRepository;
        private readonly IInstrumentProfileFactory instrumentProfileFactory;
        private readonly IVariableManager variableManager;
        private readonly IAppConfig appConfig;


        public InstrumentProfileManager(IInstrumentProfileRepository instrumentProfileRepository, IInstrumentProfileFactory instrumentProfileFactory, IVariableManager variableManager, IAppConfig appConfig)
        {
            this.instrumentProfileRepository = instrumentProfileRepository;
            this.instrumentProfileFactory = instrumentProfileFactory;
            this.variableManager = variableManager;
            this.appConfig = appConfig;

            AddRecordsFromRepository();
        }

        private void AddRecordsFromRepository()
        {
            foreach (var profileId in instrumentProfileRepository.ProfileIds)
            {
                records.TryAdd(profileId, new Record(profileId, this));
            }
        }

        public void Load()
        {
            AddRecordsFromRepository();

            var defaultProfile = appConfig.DefaultInstrumentProfileId.Length > 0 ? instrumentProfileRepository.GetProfile(appConfig.DefaultInstrumentProfileId) : null;
            if (defaultProfile != null)
            {
                CurrentProfile = defaultProfile;
            }
        }

        private void NotifyChange(IInstrumentProfileManager.IRecord record, bool removed)
        {
            _recordChanged?.Invoke(this, new IInstrumentProfileManager.RecordChangedEventArgs(record.ProfileId, removed));
        }

        private IReadOnlyInstrumentProfile CreateProfileFactoryCopy(IReadOnlyInstrumentProfile profile)
        {
            var copy = instrumentProfileFactory.Builder().Id(profile.Id).Build();

            copy.Name = profile.Name;
            copy.Description = profile.Description;
            copy.Key = profile.Key;
            copy.FocalLength = profile.FocalLength;
            copy.BitDepth = profile.BitDepth;
            copy.ElectronsPerADU = profile.ElectronsPerADU;
            copy.PixelSizeInMicrons = profile.PixelSizeInMicrons;
            if (profile.Variables != null)
            {
                copy.Variables = new List<IReadOnlyJobConfig.VariableConfig>(profile.Variables);
            }

            return copy;
        }

        private void AddOrUpdateProfile(IReadOnlyInstrumentProfile profile)
        {
            instrumentProfileRepository.AddOrUpdateProfile(profile);
            instrumentProfileRepository.Save(profile.Id);
        }

        private void SyncAndLoadProfileVariables()
        {
            var profile = CurrentProfile;
            if (profile != null)
            {
                _currentVariables ??= new();
                _currentVariables.Clear();

                if (profile.Variables != null)
                {
                    foreach (var cfg in profile.Variables)
                    {
                        if (variableManager.TryCreateVariable(cfg.Id, cfg.Name, cfg.Config, out var variable))
                        {
                            _currentVariables.Add(variable);
                        }
                        else
                        {
                            throw new Exception("Invalid or unknown variable '" + cfg.Id + "' in current profile");
                        }
                    }
                }
            }
            else
            {
                _currentVariables = null;
            }
        }

        private IReadOnlyInstrumentProfile? _currentProfile;
        public IReadOnlyInstrumentProfile? CurrentProfile
        {
            get => _currentProfile;
            set
            {
                if (!EqualityComparer<IReadOnlyInstrumentProfile?>.Default.Equals(_currentProfile, value))
                {
                    var old = _currentProfile;

                    _currentProfile = value;

                    SyncAndLoadProfileVariables();

                    _currentProfileChanged?.Invoke(this, new IInstrumentProfileManager.ProfileChangedEventArgs(old, value));
                }
            }
        }

        private List<IReadOnlyVariable>? _currentVariables;
        public IReadOnlyList<IReadOnlyVariable>? CurrentVariables => _currentVariables;

        public IReadOnlyCollection<string> ProfileIds => instrumentProfileRepository.ProfileIds;

        public bool Contains(string profileId)
        {
            return records.ContainsKey(profileId);
        }

        public IInstrumentProfileManager.IRecord? Get(string profileId)
        {
            if (records.TryGetValue(profileId, out var record))
            {
                return record;
            }
            return null;
        }

        public IInstrumentProfileManager.IRecord GetOrAdd(string profileId)
        {
            Record? newRecord = null;
            var record = records.GetOrAdd(profileId, id =>
            {
                newRecord = new Record(id, this);
                return newRecord;
            });
            if (record == newRecord)
            {
                IInstrumentProfile newProfile = instrumentProfileFactory.Builder().Id(profileId).Build();

                AddOrUpdateProfile(newProfile);

                NotifyChange(record, false);
            }
            return record;
        }

        public IInstrumentProfileManager.IRecord? Remove(string profileId)
        {
            if (records.Remove(profileId, out var record))
            {
                instrumentProfileRepository.RemoveProfile(profileId);

                NotifyChange(record, true);

                return record;
            }
            return null;
        }

        public string Save(IReadOnlyInstrumentProfile profile)
        {
            // Create copy through the factory so we can be sure
            // that it can be saved
            var copy = CreateProfileFactoryCopy(profile);
            return instrumentProfileFactory.Save(copy);
        }

        public IReadOnlyInstrumentProfile? Load(string data)
        {
            return instrumentProfileFactory.Load(data);
        }


        private EventHandler<IInstrumentProfileManager.ProfileChangedEventArgs>? _currentProfileChanged;
        public event EventHandler<IInstrumentProfileManager.ProfileChangedEventArgs> CurrentProfileChanged
        {
            add => _currentProfileChanged += value;
            remove => _currentProfileChanged -= value;
        }

        private EventHandler<IInstrumentProfileManager.RecordChangedEventArgs>? _recordChanged;
        public event EventHandler<IInstrumentProfileManager.RecordChangedEventArgs> RecordChanged
        {
            add => _recordChanged += value;
            remove => _recordChanged -= value;
        }
    }
}
