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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FitsRatingTool.Common.Models.Instrument;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IInstrumentProfileManager
    {
        public interface IRecord
        {
            string ProfileId { get; }

            [MaybeNull]
            IReadOnlyInstrumentProfile Profile { get; set; }

            bool IsValid { get; }
        }

        public class ProfileChangedEventArgs : EventArgs
        {
            public IReadOnlyInstrumentProfile? OldProfile { get; }

            public IReadOnlyInstrumentProfile? NewProfile { get; }

            public ProfileChangedEventArgs(IReadOnlyInstrumentProfile? oldProfile, IReadOnlyInstrumentProfile? newProfile)
            {
                OldProfile = oldProfile;
                NewProfile = newProfile;
            }
        }

        public class RecordChangedEventArgs : EventArgs
        {
            public string ProfileId { get; }

            public bool Removed => _removed;

            public bool AddedOrUpdated => !_removed;


            private readonly bool _removed;


            public RecordChangedEventArgs(string profileId, bool removed)
            {
                ProfileId = profileId;
                _removed = removed;
            }
        }

        IReadOnlyCollection<string> ProfileIds { get; }

        bool Contains(string profileId);

        IRecord? Get(string profileId);

        IRecord GetOrAdd(string profileId);

        IRecord? Remove(string profileId);

        string Save(IReadOnlyInstrumentProfile profile);

        IReadOnlyInstrumentProfile? Load(string data);

        IReadOnlyInstrumentProfile? CurrentProfile { get; set; }

        event EventHandler<ProfileChangedEventArgs> CurrentProfileChanged;

        event EventHandler<RecordChangedEventArgs> RecordChanged;
    }
}
