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
using FitsRatingTool.Common.Models.Instrument;
using FitsRatingTool.GuiApp.Repositories;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace FitsRatingTool.GuiApp.UI.InstrumentProfile.ViewModels
{
    public class InstrumentProfileSelectorViewModel : ViewModelBase, IInstrumentProfileSelectorViewModel
    {
        public class Factory : IInstrumentProfileSelectorViewModel.IFactory
        {
            private readonly IInstrumentProfileRepository instrumentProfileRepository;
            private readonly IInstrumentProfileViewModel.IFactory instrumentProfileFactory;

            public Factory(IInstrumentProfileRepository instrumentProfileRepository, IInstrumentProfileViewModel.IFactory instrumentProfileFactory)
            {
                this.instrumentProfileRepository = instrumentProfileRepository;
                this.instrumentProfileFactory = instrumentProfileFactory;
            }

            public IInstrumentProfileSelectorViewModel Create()
            {
                return new InstrumentProfileSelectorViewModel(instrumentProfileRepository, instrumentProfileFactory);
            }
        }

        private bool _readOnly;
        public bool ReadOnly
        {
            get => _readOnly;
            set
            {
                foreach (var profile in Profiles)
                {
                    profile.IsReadOnly = value;
                }
                this.RaiseAndSetIfChanged(ref _readOnly, value);
            }
        }

        public ReadOnlyObservableCollection<IInstrumentProfileViewModel> Profiles { get; }

        private IInstrumentProfileViewModel? _selectedProfile;
        public IInstrumentProfileViewModel? SelectedProfile
        {
            get => _selectedProfile;
            set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
        }


        private ObservableCollection<IInstrumentProfileViewModel> _profiles = new();


        private readonly IInstrumentProfileRepository instrumentProfileRepository;
        private readonly IInstrumentProfileViewModel.IFactory instrumentProfileFactory;

        private InstrumentProfileSelectorViewModel(IInstrumentProfileRepository instrumentProfileRepository, IInstrumentProfileViewModel.IFactory instrumentProfileFactory)
        {
            this.instrumentProfileRepository = instrumentProfileRepository;
            this.instrumentProfileFactory = instrumentProfileFactory;

            Profiles = new ReadOnlyObservableCollection<IInstrumentProfileViewModel>(_profiles);

            foreach (var id in instrumentProfileRepository.ProfileIds)
            {
                var profile = instrumentProfileRepository.GetProfile(id);

                if (profile != null)
                {
                    _profiles.Add(CreateProfileVM(profile));
                }
            }

            WeakEventHandlerManager.Subscribe<IInstrumentProfileRepository, IInstrumentProfileRepository.InstrumentProfileEventArgs, InstrumentProfileSelectorViewModel>(instrumentProfileRepository, nameof(instrumentProfileRepository.ProfileAdded), OnProfileEvent);
            WeakEventHandlerManager.Subscribe<IInstrumentProfileRepository, IInstrumentProfileRepository.InstrumentProfileEventArgs, InstrumentProfileSelectorViewModel>(instrumentProfileRepository, nameof(instrumentProfileRepository.ProfileRemoved), OnProfileEvent);
        }

        private IInstrumentProfileViewModel CreateProfileVM(IReadOnlyInstrumentProfile profile)
        {
            var vm = instrumentProfileFactory.Create(profile);
            vm.IsReadOnly = ReadOnly;
            return vm;
        }

        private void OnProfileEvent(object? sender, IInstrumentProfileRepository.InstrumentProfileEventArgs e)
        {
            if (e.Added)
            {
                var profile = instrumentProfileRepository.GetProfile(e.ProfileId);

                if (profile != null)
                {
                    _profiles.Add(CreateProfileVM(profile));
                }
            }
            else if (e.Removed)
            {
                foreach (var profile in _profiles)
                {
                    if (profile.SourceProfile?.Id == e.ProfileId)
                    {
                        _profiles.Remove(profile);

                        if (SelectedProfile == profile)
                        {
                            SelectedProfile = null;
                        }

                        break;
                    }
                }
            }
        }

    }
}
