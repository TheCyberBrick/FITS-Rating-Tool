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
using ReactiveUI;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.InstrumentProfile
{
    public interface IInstrumentProfileViewModel : IInstrumentProfile
    {
        public interface IFactory
        {
            IInstrumentProfileViewModel Create();

            IInstrumentProfileViewModel Create(IReadOnlyInstrumentProfile profile);
        }

        public interface IConstantViewModel : IConstant
        {
            IInstrumentProfileViewModel Profile { get; }

            bool IsNameValid { get; }

            ReactiveCommand<Unit, Unit> Remove { get; }
        }



        bool IsFocalLengthEnabled { get; set; }

        bool IsBitDepthEnabled { get; set; }

        bool IsElectronsPerADUEnabled { get; set; }

        bool IsPixelSizeInMicronsEnabled { get; set; }



        bool IsModified { get; set; }

        bool IsReadOnly { get; set; }

        bool IsNew { get; }

        IReadOnlyInstrumentProfile? SourceProfile { get; }

        new AvaloniaList<IConstantViewModel> Constants { get; }

        bool IsIdValid { get; }

        bool IsIdAvailable { get; }

        bool IsValid { get; }



        bool ResetToSourceProfile();

        ReactiveCommand<Unit, Unit> AddConstant { get; }

        ReactiveCommand<Unit, Unit> Reset { get; }
    }
}
