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
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    public class AppProfileSelectorViewModel : ViewModelBase, IAppProfileSelectorViewModel
    {
        public class Factory : IAppProfileSelectorViewModel.IFactory
        {
            private readonly IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory;
            private readonly IInstrumentProfileManager instrumentProfileManager;
            private readonly IAppConfig appConfig;

            public Factory(IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory, IInstrumentProfileManager instrumentProfileManager, IAppConfig appConfig)
            {
                this.instrumentProfileSelectorFactory = instrumentProfileSelectorFactory;
                this.instrumentProfileManager = instrumentProfileManager;
                this.appConfig = appConfig;
            }

            public IAppProfileSelectorViewModel Create()
            {
                return new AppProfileSelectorViewModel(instrumentProfileSelectorFactory, instrumentProfileManager, appConfig);
            }
        }

        public IInstrumentProfileSelectorViewModel Selector { get; }

        public ReactiveCommand<IReadOnlyInstrumentProfile?, bool> ChangeProfile { get; }

        public Interaction<Unit, bool> ChangeProfileConfirmationDialog { get; } = new();


        private volatile bool suppressChangeCommand;
        private string? prevSelectedProfileId = null;


        public AppProfileSelectorViewModel(IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory, IInstrumentProfileManager instrumentProfileManager, IAppConfig appConfig)
        {
            Selector = instrumentProfileSelectorFactory.Create();
            Selector.IsReadOnly = true;

            var initiallySelectedProfileId = instrumentProfileManager.CurrentProfile?.Id;
            if (initiallySelectedProfileId != null)
            {
                Selector.SelectById(initiallySelectedProfileId);
            }
            prevSelectedProfileId = Selector.SelectedProfile?.Id;

            ChangeProfile = ReactiveCommand.CreateFromTask<IReadOnlyInstrumentProfile?, bool>(async profile =>
            {
                bool confirmed = true;

                if (appConfig.InstrumentProfileChangeConfirmation)
                {
                    try
                    {
                        confirmed = await ChangeProfileConfirmationDialog.Handle(Unit.Default);
                    }
                    catch (UnhandledInteractionException<Unit, bool>)
                    {
                        // OK
                    }
                }

                if (confirmed)
                {
                    instrumentProfileManager.CurrentProfile = profile;

                    suppressChangeCommand = true;
                    try
                    {
                        if (profile != null)
                        {
                            Selector.SelectById(profile.Id);
                        }
                        else
                        {
                            Selector.SelectedProfile = null;
                        }
                    }
                    finally
                    {
                        suppressChangeCommand = false;
                    }
                }
                else
                {
                    suppressChangeCommand = true;
                    try
                    {
                        if (prevSelectedProfileId != null)
                        {
                            Selector.SelectById(prevSelectedProfileId);
                        }
                        else
                        {
                            Selector.SelectedProfile = null;
                        }
                    }
                    finally
                    {
                        suppressChangeCommand = false;
                    }
                }

                prevSelectedProfileId = Selector.SelectedProfile?.Id;

                return confirmed;
            });

            this.WhenAnyValue(x => x.Selector.SelectedProfile).Subscribe(profile =>
            {
                if (!suppressChangeCommand)
                {
                    ChangeProfile.Execute(profile).Subscribe();
                }
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
