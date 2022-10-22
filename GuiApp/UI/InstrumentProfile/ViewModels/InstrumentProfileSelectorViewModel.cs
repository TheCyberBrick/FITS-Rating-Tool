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
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace FitsRatingTool.GuiApp.UI.InstrumentProfile.ViewModels
{
    public class InstrumentProfileSelectorViewModel : ViewModelBase, IInstrumentProfileSelectorViewModel
    {
        public InstrumentProfileSelectorViewModel(IRegistrar<IInstrumentProfileSelectorViewModel, IInstrumentProfileSelectorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<InstrumentProfileSelectorViewModel>();
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                foreach (var profile in Profiles)
                {
                    profile.IsReadOnly = value;
                }
                this.RaiseAndSetIfChanged(ref _isReadOnly, value);
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


        private readonly IInstrumentProfileManager instrumentProfileManager;
        private readonly IContainer<IInstrumentProfileViewModel, IInstrumentProfileViewModel.OfProfile> instrumentProfileContainer;

        private InstrumentProfileSelectorViewModel(IInstrumentProfileSelectorViewModel.Of args, IInstrumentProfileManager instrumentProfileManager,
            IContainer<IInstrumentProfileViewModel, IInstrumentProfileViewModel.OfProfile> instrumentProfileContainer)
        {
            this.instrumentProfileManager = instrumentProfileManager;
            this.instrumentProfileContainer = instrumentProfileContainer;

            instrumentProfileContainer.BindTo(_profiles);

            Profiles = new ReadOnlyObservableCollection<IInstrumentProfileViewModel>(_profiles);

            WeakEventHandlerManager.Subscribe<IInstrumentProfileManager, IInstrumentProfileManager.RecordChangedEventArgs, InstrumentProfileSelectorViewModel>(instrumentProfileManager, nameof(IInstrumentProfileManager.RecordChanged), OnRecordChanged);
        }

        protected override void OnInstantiated()
        {
            foreach (var id in instrumentProfileManager.ProfileIds)
            {
                var profile = instrumentProfileManager.Get(id)?.Profile;

                if (profile != null)
                {
                    CreateProfileVM(profile);
                }
            }
        }

        private IInstrumentProfileViewModel CreateProfileVM(IReadOnlyInstrumentProfile profile)
        {
            var vm = instrumentProfileContainer.Instantiate(new IInstrumentProfileViewModel.OfProfile(profile));
            vm.IsReadOnly = IsReadOnly;
            return vm;
        }

        private void OnRecordChanged(object? sender, IInstrumentProfileManager.RecordChangedEventArgs e)
        {
            IInstrumentProfileViewModel? profile = null;
            foreach (var p in instrumentProfileContainer)
            {
                if (p.SourceProfile?.Id == e.ProfileId)
                {
                    profile = p;
                    break;
                }
            }

            if (e.Removed)
            {
                if (profile != null)
                {
                    instrumentProfileContainer.Destroy(profile);

                    if (SelectedProfile == profile)
                    {
                        SelectedProfile = null;
                    }
                }
            }
            else
            {
                if (profile != null)
                {
                    profile.ResetToSourceProfile();
                }
                else
                {
                    var newProfile = instrumentProfileManager.Get(e.ProfileId)?.Profile;
                    if (newProfile != null)
                    {
                        CreateProfileVM(newProfile);
                    }
                }
            }
        }

        public IInstrumentProfileViewModel? FindById(string profileId)
        {
            foreach (var profile in instrumentProfileContainer)
            {
                if (profile.Id == profileId)
                {
                    return profile;
                }
            }
            return null;
        }

        public IInstrumentProfileViewModel? SelectById(string profileId)
        {
            var profile = FindById(profileId);
            if (profile != null)
            {
                SelectedProfile = profile;
            }
            return profile;
        }

    }
}
