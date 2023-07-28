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

using Avalonia.Utilities;
using DryIocAttributes;
using FitsRatingTool.Common.Models.Instrument;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    [Export(typeof(IInstrumentProfileContext)), CurrentScopeReuse(AppScopes.Context.InstrumentProfile)]
    public class InstrumentProfileContext : IInstrumentProfileContext
    {
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
                    _currentProfileChanged?.Invoke(this, new IInstrumentProfileContext.ProfileChangedEventArgs(old, value));
                }
            }
        }

        private EventHandler<IInstrumentProfileContext.ProfileChangedEventArgs>? _currentProfileChanged;
        public event EventHandler<IInstrumentProfileContext.ProfileChangedEventArgs> CurrentProfileChanged
        {
            add => _currentProfileChanged += value;
            remove => _currentProfileChanged -= value;
        }


        private readonly IInstrumentProfileManager instrumentProfileManager;
        private readonly IAppConfig appConfig;

        public InstrumentProfileContext(IInstrumentProfileManager instrumentProfileManager, IAppConfig appConfig)
        {
            this.instrumentProfileManager = instrumentProfileManager;
            this.appConfig = appConfig;

            WeakEventHandlerManager.Subscribe<IInstrumentProfileManager, IInstrumentProfileManager.RecordChangedEventArgs, InstrumentProfileContext>(instrumentProfileManager, nameof(instrumentProfileManager.RecordChanged), OnProfileChanged);
        }

        public void LoadFromConfig()
        {
            var defaultProfile = appConfig.DefaultInstrumentProfileId.Length > 0 ? instrumentProfileManager.Get(appConfig.DefaultInstrumentProfileId)?.Profile : null;
            if (defaultProfile != null)
            {
                CurrentProfile = defaultProfile;
            }
        }

        public void LoadFromOther(IInstrumentProfileContext ctx)
        {
            CurrentProfile = ctx.CurrentProfile;
        }

        private void OnProfileChanged(object? sender, IInstrumentProfileManager.RecordChangedEventArgs args)
        {
            // Make sure to update the current profile when
            // its values have changed
            var changedProfile = instrumentProfileManager.Get(args.ProfileId)?.Profile;
            var currentProfile = CurrentProfile;
            if (!args.Removed && changedProfile != null && currentProfile != null && changedProfile.Id == currentProfile.Id)
            {
                CurrentProfile = changedProfile;
            }
        }
    }
}
