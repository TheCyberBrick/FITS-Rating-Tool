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
using ReactiveUI;
using System;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.InstrumentProfile
{
    public interface IInstrumentProfileConfiguratorViewModel
    {
        public interface IFactory
        {
            IInstrumentProfileConfiguratorViewModel Create();
        }

        IInstrumentProfileSelectorViewModel Selector { get; }

        bool HasProfile { get; }

        Interaction<IReadOnlyInstrumentProfile, bool> DeleteConfirmationDialog { get; }

        Interaction<Unit, bool> DiscardConfirmationDialog { get; }

        ReactiveCommand<Unit, Unit> New { get; }

        ReactiveCommand<Unit, Unit> Save { get; }

        ReactiveCommand<Unit, Unit> Delete { get; }

        ReactiveCommand<Unit, bool> Cancel { get; }

        #region Import/Export
        public class ImportResult
        {
            public IInstrumentProfileViewModel? Profile { get; }

            public Exception? Error { get; }

            public string? ErrorMessage { get; }

            public bool Successful => Profile != null;

            public ImportResult(IInstrumentProfileViewModel? profile)
            {
                Profile = profile;
                Error = null;
                ErrorMessage = null;
            }

            public ImportResult(string errorMessage)
            {
                Profile = null;
                Error = null;
                ErrorMessage = errorMessage;
            }

            public ImportResult(Exception? error)
            {
                Profile = null;
                Error = error;
                ErrorMessage = null;
            }
        }

        public class ExportResult
        {
            public Exception? Error { get; }

            public string? ErrorMessage { get; }

            public bool Successful => Error == null && ErrorMessage == null;

            public ExportResult()
            {
                Error = null;
                ErrorMessage = null;
            }

            public ExportResult(string errorMessage)
            {
                Error = null;
                ErrorMessage = errorMessage;
            }

            public ExportResult(Exception? error)
            {
                Error = error;
                ErrorMessage = null;
            }
        }

        ReactiveCommand<Unit, ImportResult> ImportWithOpenFileDialog { get; }

        ReactiveCommand<Unit, ExportResult> ExportWithSaveFileDialog { get; }

        Interaction<Unit, string> ImportOpenFileDialog { get; }

        Interaction<Unit, string> ExportSaveFileDialog { get; }

        Interaction<ImportResult, Unit> ImportResultDialog { get; }

        Interaction<ExportResult, Unit> ExportResultDialog { get; }
        #endregion
    }
}
