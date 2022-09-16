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
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using ReactiveUI;
using System;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    public class AppProfileSelectorViewModel : ViewModelBase, IAppProfileSelectorViewModel
    {
        public class Factory : IAppProfileSelectorViewModel.IFactory
        {
            private readonly IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory;
            private readonly IInstrumentProfileManager instrumentProfileManager;

            public Factory(IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory, IInstrumentProfileManager instrumentProfileManager)
            {
                this.instrumentProfileSelectorFactory = instrumentProfileSelectorFactory;
                this.instrumentProfileManager = instrumentProfileManager;
            }

            public IAppProfileSelectorViewModel Create()
            {
                return new AppProfileSelectorViewModel(instrumentProfileSelectorFactory, instrumentProfileManager);
            }
        }

        public IInstrumentProfileSelectorViewModel Selector { get; }

        public AppProfileSelectorViewModel(IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory, IInstrumentProfileManager instrumentProfileManager)
        {
            Selector = instrumentProfileSelectorFactory.Create();
            Selector.IsReadOnly = true;

            var initiallySelectedProfileId = instrumentProfileManager.CurrentProfile?.Id;
            if (initiallySelectedProfileId != null)
            {
                Selector.SelectById(initiallySelectedProfileId);
            }

            this.WhenAnyValue(x => x.Selector.SelectedProfile).Subscribe(profile =>
            {
                instrumentProfileManager.CurrentProfile = profile;
            });

            WeakEventHandlerManager.Subscribe<IInstrumentProfileManager, IInstrumentProfileManager.ProfileChangedEventArgs, AppProfileSelectorViewModel>(instrumentProfileManager, nameof(instrumentProfileManager.CurrentProfileChanged), OnCurrentProfileChanged);
        }

        private void OnCurrentProfileChanged(object? sender, IInstrumentProfileManager.ProfileChangedEventArgs e)
        {
            var newProfile = e.NewProfile;
            if (newProfile != null)
            {
                Selector.SelectById(newProfile.Id);
            }
            else
            {
                Selector.SelectedProfile = null;
            }
        }
    }
}
