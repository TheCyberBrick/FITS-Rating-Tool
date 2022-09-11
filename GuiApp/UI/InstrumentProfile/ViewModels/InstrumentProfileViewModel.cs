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

using Avalonia.Collections;
using FitsRatingTool.Common.Models.Instrument;
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.InstrumentProfile.ViewModels
{
    public class InstrumentProfileViewModel : ViewModelBase, IInstrumentProfileViewModel
    {
        public class Factory : IInstrumentProfileViewModel.IFactory
        {
            private readonly IInstrumentProfileManager instrumentProfileManager;

            public Factory(IInstrumentProfileManager instrumentProfileManager)
            {
                this.instrumentProfileManager = instrumentProfileManager;
            }

            public IInstrumentProfileViewModel Create()
            {
                return new InstrumentProfileViewModel(instrumentProfileManager, null);
            }

            public IInstrumentProfileViewModel Create(IReadOnlyInstrumentProfile profile)
            {
                return new InstrumentProfileViewModel(instrumentProfileManager, profile);
            }
        }

        private class ConstantViewModel : ViewModelBase, IInstrumentProfileViewModel.IConstantViewModel
        {
            public IInstrumentProfileViewModel Profile { get; }

            private readonly ObservableAsPropertyHelper<bool> _isNameValid;
            public bool IsNameValid => _isNameValid.Value;

            private string _name = "";
            public string Name
            {
                get => _name;
                set => this.RaiseAndSetIfChanged(ref _name, value);
            }

            private double _value;
            public double Value
            {
                get => _value;
                set => this.RaiseAndSetIfChanged(ref _value, value);
            }

            public ReactiveCommand<Unit, Unit> Remove { get; } = ReactiveCommand.Create(() => { });

            public ConstantViewModel(InstrumentProfileViewModel profile)
            {
                Profile = profile;
                _isNameValid = this.WhenAnyValue(x => x.Name, ValidateName).ToProperty(this, x => x.IsNameValid);

                this.WhenAnyValue(x => x.IsNameValid).Subscribe(_ => profile.ValidateConstants());

                this.WhenAnyValue(x => x.Name).Subscribe(_ => Profile.IsModified = true);
                this.WhenAnyValue(x => x.Value).Subscribe(_ => Profile.IsModified = true);
            }

            private bool ValidateName(string name)
            {
                return name.Length > 0 && char.IsLetter(name[0]) && name.All(x => char.IsLetterOrDigit(x));
            }
        }

        private string _id = "";
        public string Id
        {
            get => _id;
            set => this.RaiseAndSetIfChanged(ref _id, value);
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        private string _key = "";
        public string Key
        {
            get => _key;
            set => this.RaiseAndSetIfChanged(ref _key, value);
        }

        private float? _focalLength;
        public float? FocalLength
        {
            get => _focalLength;
            set => this.RaiseAndSetIfChanged(ref _focalLength, value);
        }

        private int? _bitDepth;
        public int? BitDepth
        {
            get => _bitDepth;
            set => this.RaiseAndSetIfChanged(ref _bitDepth, value);
        }

        private float? _electronsPerADU;
        public float? ElectronsPerADU
        {
            get => _electronsPerADU;
            set => this.RaiseAndSetIfChanged(ref _electronsPerADU, value);
        }

        private float? _pixelSizeInMicrons;
        public float? PixelSizeInMicrons
        {
            get => _pixelSizeInMicrons;
            set => this.RaiseAndSetIfChanged(ref _pixelSizeInMicrons, value);
        }



        private bool _isFocalLengthEnabled;
        public bool IsFocalLengthEnabled
        {
            get => _isFocalLengthEnabled;
            set => this.RaiseAndSetIfChanged(ref _isFocalLengthEnabled, value);
        }

        private bool _isBitDepthEnabled;
        public bool IsBitDepthEnabled
        {
            get => _isBitDepthEnabled;
            set => this.RaiseAndSetIfChanged(ref _isBitDepthEnabled, value);
        }

        private bool _isElectronsPerADUEnabled;
        public bool IsElectronsPerADUEnabled
        {
            get => _isElectronsPerADUEnabled;
            set => this.RaiseAndSetIfChanged(ref _isElectronsPerADUEnabled, value);
        }

        private bool _isPixelSizeInMicronsEnabled;
        public bool IsPixelSizeInMicronsEnabled
        {
            get => _isPixelSizeInMicronsEnabled;
            set => this.RaiseAndSetIfChanged(ref _isPixelSizeInMicronsEnabled, value);
        }



        public AvaloniaList<IInstrumentProfileViewModel.IConstantViewModel> Constants { get; } = new();

        IReadOnlyList<IInstrumentProfile.IConstant> IInstrumentProfile.Constants
        {
            get => Constants;
            set
            {
                Constants.Clear();
                foreach (var c in value)
                {
                    var constVm = CreateConstant();
                    constVm.Name = c.Name;
                    constVm.Value = c.Value;
                    Constants.Add(constVm);
                }
            }
        }

        IReadOnlyList<IReadOnlyInstrumentProfile.IReadOnlyConstant> IReadOnlyInstrumentProfile.Constants => Constants;


        private bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set => this.RaiseAndSetIfChanged(ref _isModified, value);
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
        }

        public bool IsNew { get; }

        private IReadOnlyInstrumentProfile? _sourceProfile;
        public IReadOnlyInstrumentProfile? SourceProfile
        {
            get => _sourceProfile;
            private set => this.RaiseAndSetIfChanged(ref _sourceProfile, value);
        }

        private readonly ObservableAsPropertyHelper<bool> _isIdValid;
        public bool IsIdValid => _isIdValid.Value;

        private readonly ObservableAsPropertyHelper<bool> _isIdAvailable;
        public bool IsIdAvailable => _isIdAvailable.Value;

        private readonly ObservableAsPropertyHelper<bool> _isValid;
        public bool IsValid => _isValid.Value;

        private bool _isConstantNameValid;
        private bool IsConstantNameValid
        {
            get => _isConstantNameValid;
            set => this.RaiseAndSetIfChanged(ref _isConstantNameValid, value);
        }

        public ReactiveCommand<Unit, Unit> AddConstant { get; }

        public ReactiveCommand<Unit, Unit> Reset { get; }


        private readonly IInstrumentProfileManager instrumentProfileManager;

        private InstrumentProfileViewModel(IInstrumentProfileManager instrumentProfileManager, IReadOnlyInstrumentProfile? profile)
        {
            this.instrumentProfileManager = instrumentProfileManager;

            IsNew = profile == null;

            SourceProfile = profile;

            AddConstant = ReactiveCommand.Create(() =>
            {
                CreateConstant();
            });

            Reset = ReactiveCommand.Create(() =>
            {
                ResetToSourceProfile();
            }, Observable.CombineLatest(Observable.Return(!IsNew), this.WhenAnyValue(x => x.IsModified), (a, b) => a && b));

            var isIdAvailable = this.WhenAnyValue(x => x.Id, x => (!IsNew && x == profile?.Id) || !instrumentProfileManager.Contains(x));
            _isIdAvailable = isIdAvailable.ToProperty(this, x => x.IsIdAvailable);

            var isIdValidated = this.WhenAnyValue(x => x.Id, ValidateId);
            _isIdValid = Observable.CombineLatest(isIdValidated, isIdAvailable, (a, b) => a && b).ToProperty(this, x => x.IsIdValid);

            _isValid = Observable.CombineLatest(this.WhenAnyValue(x => x.IsIdValid), this.WhenAnyValue(x => x.IsConstantNameValid), (a, b) => a && b).ToProperty(this, x => x.IsValid);

            this.WhenAnyValue(x => x.IsFocalLengthEnabled).Select(x => !x).Subscribe(_ => FocalLength = null);
            this.WhenAnyValue(x => x.IsBitDepthEnabled).Select(x => !x).Subscribe(_ => BitDepth = null);
            this.WhenAnyValue(x => x.IsElectronsPerADUEnabled).Select(x => !x).Subscribe(_ => ElectronsPerADU = null);
            this.WhenAnyValue(x => x.IsPixelSizeInMicronsEnabled).Select(x => !x).Subscribe(_ => PixelSizeInMicrons = null);

            this.WhenAnyValue(x => x.Id).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.Name).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.Description).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.Key).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.IsFocalLengthEnabled).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.FocalLength).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.IsBitDepthEnabled).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.BitDepth).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.IsElectronsPerADUEnabled).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.ElectronsPerADU).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.IsPixelSizeInMicronsEnabled).Subscribe(_ => IsModified = true);
            this.WhenAnyValue(x => x.PixelSizeInMicrons).Subscribe(_ => IsModified = true);

            Constants.CollectionChanged += (sender, e) =>
            {
                IsModified = true;
                ValidateConstants();
            };

            if (profile != null)
            {
                LoadFromProfile(profile);
            }

            ValidateConstants();
        }

        private bool ValidateId(string id)
        {
            return id.Length > 0 && id.All(x => char.IsLetterOrDigit(x) || x == '_');
        }

        private void LoadFromProfile(IReadOnlyInstrumentProfile profile)
        {
            // Fetch new profile values if available
            var newProfile = instrumentProfileManager.Get(profile.Id)?.Profile;
            if (newProfile != null)
            {
                SourceProfile = profile = newProfile;
            }

            using (DelayChangeNotifications())
            {
                Id = profile.Id;
                Name = profile.Name;
                Description = profile.Description;
                Key = profile.Key;
            }

            if (profile.FocalLength != null)
            {
                IsFocalLengthEnabled = true;
                FocalLength = profile.FocalLength;
            }
            else
            {
                IsFocalLengthEnabled = false;
            }

            if (profile.BitDepth != null)
            {
                IsBitDepthEnabled = true;
                BitDepth = profile.BitDepth;
            }
            else
            {
                IsBitDepthEnabled = false;
            }

            if (profile.ElectronsPerADU != null)
            {
                IsElectronsPerADUEnabled = true;
                ElectronsPerADU = profile.ElectronsPerADU;
            }
            else
            {
                IsElectronsPerADUEnabled = false;
            }

            if (profile.PixelSizeInMicrons != null)
            {
                IsPixelSizeInMicronsEnabled = true;
                PixelSizeInMicrons = profile.PixelSizeInMicrons;
            }
            else
            {
                IsPixelSizeInMicronsEnabled = false;
            }

            Constants.Clear();
            foreach (var constant in profile.Constants)
            {
                var constantVm = CreateConstant();
                constantVm.Name = constant.Name;
                constantVm.Value = constant.Value;
            }

            IsModified = false;
        }

        private ConstantViewModel CreateConstant()
        {
            var constant = new ConstantViewModel(this);

            constant.Remove.Subscribe(_ =>
            {
                Constants.Remove(constant);
            });

            Constants.Add(constant);

            return constant;
        }

        public bool ResetToSourceProfile()
        {
            if (SourceProfile != null)
            {
                LoadFromProfile(SourceProfile);
                return true;
            }
            return false;
        }

        private void ValidateConstants()
        {
            bool valid = true;
            foreach (var constant in Constants)
            {
                if (!constant.IsNameValid)
                {
                    valid = false;
                    break;
                }
            }
            IsConstantNameValid = valid;
        }
    }
}
