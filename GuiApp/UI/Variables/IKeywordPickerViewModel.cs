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

using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.UI.Variables
{
    public interface IKeywordPickerViewModel
    {
        public record OfFile(string File, bool AutoReset = false);

        public record OfFiles(IEnumerable<string> Files, bool AutoReset = false);

        public record OfAllFiles(bool AutoReset = false);

        public record OfCurrentlySelectedFile(bool AutoReset = false);

        IReadOnlyList<string> Keywords { get; }

        string? SelectedKeyword { get; set; }

        void Reset(bool keepSelection = false);

        bool Select(string? keyword);
    }
}
