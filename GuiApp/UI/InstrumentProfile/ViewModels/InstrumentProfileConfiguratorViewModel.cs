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
using FitsRatingTool.GuiApp.Repositories;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System;
using static FitsRatingTool.GuiApp.UI.InstrumentProfile.IInstrumentProfileConfiguratorViewModel;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.UI.InstrumentProfile.ViewModels
{
    public class InstrumentProfileConfiguratorViewModel : ViewModelBase, IInstrumentProfileConfiguratorViewModel
    {
        public class Factory : IInstrumentProfileConfiguratorViewModel.IFactory
        {
            private readonly IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory;
            private readonly IInstrumentProfileViewModel.IFactory instrumentProfileVMFactory;
            private readonly IInstrumentProfileRepository instrumentProfileRepository;
            private readonly IInstrumentProfileFactory instrumentProfileFactory;

            public Factory(IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory, IInstrumentProfileViewModel.IFactory instrumentProfileVMFactory, IInstrumentProfileRepository instrumentProfileRepository,
                IInstrumentProfileFactory instrumentProfileFactory)
            {
                this.instrumentProfileSelectorFactory = instrumentProfileSelectorFactory;
                this.instrumentProfileVMFactory = instrumentProfileVMFactory;
                this.instrumentProfileRepository = instrumentProfileRepository;
                this.instrumentProfileFactory = instrumentProfileFactory;
            }

            public IInstrumentProfileConfiguratorViewModel Create()
            {
                return new InstrumentProfileConfiguratorViewModel(instrumentProfileSelectorFactory, instrumentProfileVMFactory, instrumentProfileRepository, instrumentProfileFactory);
            }
        }


        public IInstrumentProfileSelectorViewModel Selector { get; }

        private readonly ObservableAsPropertyHelper<bool> _hasProfile;
        public bool HasProfile => _hasProfile.Value;

        public Interaction<IReadOnlyInstrumentProfile, bool> DeleteConfirmationDialog { get; } = new();

        public Interaction<Unit, bool> DiscardConfirmationDialog { get; } = new();

        public ReactiveCommand<Unit, Unit> New { get; }

        public ReactiveCommand<Unit, Unit> Save { get; }

        public ReactiveCommand<Unit, Unit> Delete { get; }

        public ReactiveCommand<Unit, bool> Cancel { get; }


        public ReactiveCommand<Unit, ImportResult> ImportWithOpenFileDialog { get; }

        public ReactiveCommand<Unit, ExportResult> ExportWithSaveFileDialog { get; }

        public Interaction<Unit, string> ImportOpenFileDialog { get; } = new();

        public Interaction<Unit, string> ExportSaveFileDialog { get; } = new();

        public Interaction<ImportResult, Unit> ImportResultDialog { get; } = new();

        public Interaction<ExportResult, Unit> ExportResultDialog { get; } = new();



        private readonly IInstrumentProfileFactory instrumentProfileFactory;

        private InstrumentProfileConfiguratorViewModel(IInstrumentProfileSelectorViewModel.IFactory instrumentProfileSelectorFactory, IInstrumentProfileViewModel.IFactory instrumentProfileVMFactory, IInstrumentProfileRepository instrumentProfileRepository,
            IInstrumentProfileFactory instrumentProfileFactory)
        {
            this.instrumentProfileFactory = instrumentProfileFactory;

            Selector = instrumentProfileSelectorFactory.Create();

            var hasProfile = this.WhenAnyValue(x => x.Selector.SelectedProfile, (IInstrumentProfileViewModel? x) => x != null);
            _hasProfile = hasProfile.ToProperty(this, x => x.HasProfile);

            var isSelectedProfileExisting = this.WhenAnyValue(x => x.Selector.SelectedProfile!.SourceProfile, (IReadOnlyInstrumentProfile? x) => x != null);
            var isSelectedProfileModified = this.WhenAnyValue(x => x.Selector.SelectedProfile!.IsModified);
            var isSelectedProfileValid = this.WhenAnyValue(x => x.Selector.SelectedProfile!.IsValid);

            this.WhenAnyValue(x => x.Selector.SelectedProfile).Subscribe(newProfile =>
            {
                foreach (var profile in Selector.Profiles)
                {
                    if (newProfile != profile)
                    {
                        profile.ResetToSourceProfile();
                    }
                }
            });

            New = ReactiveCommand.CreateFromTask(async () =>
            {
                var discard = true;

                if (Selector.SelectedProfile?.IsModified ?? false)
                {
                    try
                    {
                        discard = await DiscardConfirmationDialog.Handle(Unit.Default);
                    }
                    catch (UnhandledInteractionException<Unit, bool>)
                    {
                        // OK
                    }
                }

                if (discard)
                {
                    Selector.SelectedProfile = null;
                    Selector.SelectedProfile = instrumentProfileVMFactory.Create();
                }
            });

            Save = ReactiveCommand.Create(() =>
            {
                var profile = Selector.SelectedProfile;
                if (profile != null)
                {
                    instrumentProfileRepository.AddOrUpdateProfile(CreateProfileFromVM(profile));
                    instrumentProfileRepository.Save(profile.Id);

                    profile.ResetToSourceProfile();

                    // If a new profile was added then select it
                    // to replace the current temporary one with
                    // the new one
                    foreach (var p in Selector.Profiles)
                    {
                        if (p.Id == profile.Id)
                        {
                            Selector.SelectedProfile = p;
                        }
                    }
                }
            }, isSelectedProfileValid);

            Delete = ReactiveCommand.CreateFromTask(async () =>
            {
                var source = Selector.SelectedProfile?.SourceProfile;

                if (source != null)
                {
                    var delete = true;

                    try
                    {
                        delete = await DeleteConfirmationDialog.Handle(source);
                    }
                    catch (UnhandledInteractionException<IReadOnlyInstrumentProfile, bool>)
                    {
                        // OK
                    }

                    if (delete)
                    {
                        var profile = Selector.SelectedProfile;
                        if (profile != null)
                        {
                            instrumentProfileRepository.RemoveProfile(profile.Id);
                            Selector.SelectedProfile = null;
                        }
                    }
                }
            }, isSelectedProfileExisting);

            Cancel = ReactiveCommand.CreateFromTask(async () =>
            {
                var discard = true;

                if (Selector.SelectedProfile?.IsModified ?? false)
                {
                    try
                    {
                        discard = await DiscardConfirmationDialog.Handle(Unit.Default);
                    }
                    catch (UnhandledInteractionException<Unit, bool>)
                    {
                        // OK
                    }
                }

                return discard;
            });

            ImportWithOpenFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                ImportResult? result = null;

                Exception? error = null;

                var file = await ImportOpenFileDialog.Handle(Unit.Default);
                if (file.Length > 0)
                {
                    try
                    {
                        var profile = instrumentProfileFactory.Load(File.ReadAllText(file, Encoding.UTF8));

                        if (profile != null)
                        {
                            if (instrumentProfileRepository.GetProfile(profile.Id) != null)
                            {
                                result = new ImportResult($"A profile with ID '{profile.Id}' already exists");
                            }
                            else
                            {
                                instrumentProfileRepository.AddOrUpdateProfile(profile);
                                instrumentProfileRepository.Save(profile.Id);

                                IInstrumentProfileViewModel? vm = null;

                                foreach (var p in Selector.Profiles)
                                {
                                    if (p.Id == profile.Id)
                                    {
                                        vm = p;
                                        break;
                                    }
                                }

                                result = new ImportResult(vm);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                }

                result ??= new ImportResult(error);

                try
                {
                    await ImportResultDialog.Handle(result);
                }
                catch (UnhandledInteractionException<ImportResult, Unit>)
                {
                    // OK
                }

                return result;
            });

            ExportWithSaveFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                ExportResult? result = null;

                var profileVm = Selector.SelectedProfile;

                Exception? error = null;

                if (profileVm != null)
                {
                    var file = await ExportSaveFileDialog.Handle(Unit.Default);
                    if (file.Length > 0)
                    {
                        try
                        {
                            var profile = CreateProfileFromVM(profileVm);

                            var data = instrumentProfileFactory.Save(profile);

                            await File.WriteAllTextAsync(file, data);

                            result = new ExportResult();
                        }
                        catch (Exception ex)
                        {
                            error = ex;
                        }
                    }
                }

                result ??= new ExportResult(error);

                try
                {
                    await ExportResultDialog.Handle(result);
                }
                catch (UnhandledInteractionException<ExportResult, Unit>)
                {
                    // OK
                }

                return result;
            }, hasProfile);
        }

        private IInstrumentProfile CreateProfileFromVM(IInstrumentProfileViewModel vm)
        {
            var profile = instrumentProfileFactory.Create();

            profile.Id = vm.Id;
            profile.Name = vm.Name;
            profile.Description = vm.Description;
            profile.Key = vm.Key;
            profile.FocalLength = vm.FocalLength;
            profile.BitDepth = vm.BitDepth;
            profile.ElectronsPerADU = vm.ElectronsPerADU;
            profile.PixelSizeInMicrons = vm.PixelSizeInMicrons;

            var constants = new List<IInstrumentProfile.IConstant>();

            foreach (var constant in vm.Constants)
            {
                var pconstant = new IInstrumentProfile.Constant()
                {
                    Name = constant.Name,
                    Value = constant.Value
                };

                constants.Add(pconstant);
            }

            profile.Constants = constants;

            return profile;
        }
    }
}
