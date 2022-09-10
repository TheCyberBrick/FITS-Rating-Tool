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

namespace FitsRatingTool.Common.Models.Instrument
{
    public interface IInstrumentProfile : IReadOnlyInstrumentProfile
    {
        public interface IConstant : IReadOnlyConstant
        {
            new string Name { get; set; }

            new double Value { get; set; }
        }

        public class Constant : IConstant
        {
            public string Name { get; set; } = "";

            public double Value { get; set; }
        }

        new string Id { get; set; }

        new string Name { get; set; }

        new string Description { get; set; }

        new string Key { get; set; }

        new float? FocalLength { get; set; }

        new int? BitDepth { get; set; }

        new float? ElectronsPerADU { get; set; }

        new float? PixelSizeInMicrons { get; set; }

        new IReadOnlyList<IConstant> Constants { get; set; }
    }
}
