﻿/*
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

using FitsRatingTool.Common.Services;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IEvaluationManager
    {
        IEvaluationContext? EvaluationContext { get; set; }

        IVariableContext? VariableContext { get; set; }

        bool AutoUpdateRatings { get; set; }

        void InvalidateStatistics();

        void InvalidateEvaluation();

        Task UpdateRatingsAsync(string? specificGroupKey = null, IEvaluationService.IEvaluator? evaluator = null);
    }
}
