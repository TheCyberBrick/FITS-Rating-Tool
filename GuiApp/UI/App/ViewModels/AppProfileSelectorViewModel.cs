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

using DryIocAttributes;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using FitsRatingTool.IoC;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reactive;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    [Export(typeof(IAppProfileSelectorViewModel)), TransientReuse]
    public class AppProfileSelectorViewModel : ViewModelBase, IAppProfileSelectorViewModel
    {
        public AppProfileSelectorViewModel(IRegistrar<IAppProfileSelectorViewModel, IAppProfileSelectorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<AppProfileSelectorViewModel>();
        }


        public IInstrumentProfileSelectorViewModel Selector { get; private set; } = null!;

        public ReactiveCommand<IInstrumentProfileViewModel?, bool> ChangeProfile { get; }

        public Interaction<Unit, bool> ChangeProfileConfirmationDialog { get; } = new();

        public Interaction<List<string>, bool> ChangeProfileMissingVariablesWarningDialog { get; } = new();


        private volatile bool suppressChangeCommand;
        private string? prevSelectedProfileId = null;


        private AppProfileSelectorViewModel(IAppProfileSelectorViewModel.Of args,
            IContainer<IInstrumentProfileSelectorViewModel, IInstrumentProfileSelectorViewModel.Of> instrumentProfileSelectorContainer,
            IInstrumentProfileContext instrumentProfileContext, IEvaluationContext evaluationContext, IAppConfig appConfig)
        {
            instrumentProfileSelectorContainer.Singleton().Inject(new IInstrumentProfileSelectorViewModel.Of(), vm =>
            {
                Selector = vm;
                Selector.IsReadOnly = true;

                var initiallySelectedProfileId = instrumentProfileContext.CurrentProfile?.Id;
                if (initiallySelectedProfileId != null)
                {
                    Selector.SelectById(initiallySelectedProfileId);
                }
                prevSelectedProfileId = Selector.SelectedProfile?.Id;
            });

            ChangeProfile = ReactiveCommand.CreateFromTask<IInstrumentProfileViewModel?, bool>(async profile =>
            {
                bool confirmed = true;

                if (appConfig.InstrumentProfileChangeConfirmation)
                {
                    try
                    {
                        confirmed &= await ChangeProfileConfirmationDialog.Handle(Unit.Default);
                    }
                    catch (UnhandledInteractionException<Unit, bool>)
                    {
                        // OK
                    }

                    if (confirmed && evaluationContext.LoadedInstrumentProfile == null /* Checking for missing vars is only necessary if formula won't be reset */)
                    {
                        var currentEvaluator = evaluationContext.CurrentEvaluator;
                        if (currentEvaluator != null)
                        {
                            var availableVariables = new HashSet<string>();
                            if (profile != null)
                            {
                                foreach (var variableVm in profile.Variables)
                                {
                                    try
                                    {
                                        var variable = variableVm.Editor.Configurator?.CreateVariable();
                                        if (variable != null)
                                        {
                                            availableVariables.Add(variable.Name);
                                        }
                                    }
                                    catch
                                    {
                                        // OK, we only care about valid variables
                                    }
                                }
                            }

                            var missingVariables = new List<string>();
                            foreach (var requiredVariable in currentEvaluator.RequiredConstants)
                            {
                                if (!availableVariables.Contains(requiredVariable))
                                {
                                    missingVariables.Add(requiredVariable);
                                }
                            }

                            if (missingVariables.Count > 0)
                            {
                                try
                                {
                                    confirmed &= await ChangeProfileMissingVariablesWarningDialog.Handle(missingVariables);
                                }
                                catch (UnhandledInteractionException<Unit, bool>)
                                {
                                    // OK
                                }
                            }
                        }
                    }
                }

                if (confirmed)
                {
                    try
                    {
                        instrumentProfileContext.CurrentProfile = profile?.CreateProfile();
                    }
                    catch
                    {
                        // If profile is invalid we fallback to the
                        // source profile
                        instrumentProfileContext.CurrentProfile = profile?.SourceProfile;
                    }

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

            SubscribeToEvent<IInstrumentProfileContext, IInstrumentProfileContext.ProfileChangedEventArgs, AppProfileSelectorViewModel>(instrumentProfileContext, nameof(instrumentProfileContext.CurrentProfileChanged), OnCurrentProfileChanged);
        }

        protected override void OnInstantiated()
        {
            this.WhenAnyValue(x => x.Selector.SelectedProfile).Subscribe(profile =>
            {
                if (!suppressChangeCommand)
                {
                    ChangeProfile.Execute(profile).Subscribe();
                }
            });
        }

        private void OnCurrentProfileChanged(object? sender, IInstrumentProfileContext.ProfileChangedEventArgs e)
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
