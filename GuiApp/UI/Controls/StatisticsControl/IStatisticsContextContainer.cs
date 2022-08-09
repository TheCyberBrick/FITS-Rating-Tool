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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using System.Collections.Generic;
using FitsRatingTool.GuiApp.UI.Controls.ContextualItemsControl;

namespace FitsRatingTool.GuiApp.UI.Controls.StatisticsControl
{
    public interface IStatisticsContextContainer : IContextContainer, ISelectable
    {
        #region +++ Input Bindings +++
        Dictionary<string, IBinding> ValueBindings { get; set; }

        Dictionary<string, IBinding> EnabledBindings { get; set; }

        object? InputValue { get; set; }

        bool InputEnabled { get; set; }
        #endregion

        #region +++ Output Bindings +++
        object? Value { get; }

        bool Enabled { get; }

        double X { get; set; }

        double Y { get; set; }

        double CanvasX { get; set; }

        double CanvasY { get; set; }

        double CanvasWidth { get; set; }

        double CanvasHeight { get; set; }

        Point StartPoint { get; set; }

        Point EndPoint { get; set; }

        bool HasPreviousPoint { get; set; }
        #endregion

        void BindToDataKey(string? dataKey);

        void Unbind();
    }
}
