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

namespace FitsRatingTool.GuiApp
{
    public static class AppScopes
    {
        public static class Service
        {
            public const string Windowing = "FRT.Service.Windowing";
        }

        public static class Context
        {
            public const string Evaluation = "FRT.Context.Evaluation";
            public const string Variable = "FRT.Context.Variable";
            public const string InstrumentProfile = "FRT.Context.InstrumentProfile";
        }
    }
}
