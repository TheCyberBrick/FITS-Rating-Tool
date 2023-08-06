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

using FitsRatingTool.Common.Models.Evaluation;

namespace FitsRatingTool.Common.Models.Instrument
{
    public interface IInstrumentProfile : IReadOnlyInstrumentProfile
    {
        #region Profile
        new string Id { get; set; }

        new string Name { get; set; }

        new string Description { get; set; }

        new string Key { get; set; }
        #endregion

        #region Instrument
        new float? FocalLength { get; set; }

        new int? BitDepth { get; set; }

        new float? ElectronsPerADU { get; set; }

        new float? PixelSizeInMicrons { get; set; }
        #endregion

        #region Grouping
        new IReadOnlyCollection<string>? GroupingKeys { get; set; }
        #endregion

        #region Variables
        new IReadOnlyList<IReadOnlyJobConfig.VariableConfig> Variables { get; set; }
        #endregion

        #region Evaluation Formula
        new string EvaluationFormulaPath { get; set; }
        #endregion
    }
}
