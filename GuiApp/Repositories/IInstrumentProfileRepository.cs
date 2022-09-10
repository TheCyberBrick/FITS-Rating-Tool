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
using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.Repositories
{
    public interface IInstrumentProfileRepository
    {
        public class InstrumentProfileEventArgs : EventArgs
        {
            public string ProfileId { get; private set; }

            public bool Changed { get; private set; }

            public bool Added { get; private set; }

            public bool Removed { get; private set; }

            public InstrumentProfileEventArgs(string profileId, bool changed, bool added, bool removed)
            {
                ProfileId = profileId;
                Changed = changed;
                Added = added;
                Removed = removed;
            }
        }


        IReadOnlyCollection<string> ProfileIds { get; }

        IReadOnlyInstrumentProfile? GetProfile(string id);

        void AddOrUpdateProfile(IReadOnlyInstrumentProfile profile);

        void RemoveProfile(string id);

        void Save(string id);

        void SaveAll();


        event EventHandler<InstrumentProfileEventArgs> ProfileChanged;

        event EventHandler<InstrumentProfileEventArgs> ProfileAdded;

        event EventHandler<InstrumentProfileEventArgs> ProfileRemoved;
    }
}
