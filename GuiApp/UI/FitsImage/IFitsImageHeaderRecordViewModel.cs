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

using FitsRatingTool.FitsLoader.Models;

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public interface IFitsImageHeaderRecordViewModel : IFitsImageHeaderRecord
    {
        public record OfRecord(FitsImageHeaderRecord Record);

        // Data grid sorting only seems to work if all
        // properties are explicitly declared in the
        // interface the data grid is bound to...
        new string Keyword { get; }
        new string Value { get; }
        new string Comment { get; }
    }
}
