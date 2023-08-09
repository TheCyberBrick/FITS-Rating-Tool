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

namespace FitsRatingTool.GuiApp.UI.Evaluation
{
    public interface IEvaluationFormulaViewModel
    {
        public record Of();

        ReactiveCommand<Unit, Unit> Reset { get; }

        Interaction<Unit, bool> ResetConfirmationDialog { get; }

        string? RatingFormula { get; set; }

        bool IsFormulaUsingAggregateFunctions { get; }

        bool AutoUpdateRatings { get; set; }

        ReactiveCommand<Unit, Unit> UpdateRatings { get; }

        string? RatingFormulaError { get; }

        Interaction<string, Unit> RatingFormulaErrorDialog { get; }

        bool IsModified { get; }

        string? LoadedFile { get; }

        IReadOnlyInstrumentProfile? LoadedInstrumentProfile { get; }

        ReactiveCommand<Unit, Unit> LoadFormulaWithOpenFileDialog { get; }

        Interaction<Unit, string> LoadFormulaOpenFileDialog { get; }

        ReactiveCommand<Unit, Unit> SaveFormula { get; }

        ReactiveCommand<Unit, Unit> SaveFormulaWithSaveFileDialog { get; }

        Interaction<Unit, string> SaveFormulaSaveFileDialog { get; }



        #region +++ Grouping +++
        AvaloniaList<string> GroupKeys { get; }

        IJobGroupingConfiguratorViewModel GroupingConfigurator { get; }
        #endregion
    }
}
