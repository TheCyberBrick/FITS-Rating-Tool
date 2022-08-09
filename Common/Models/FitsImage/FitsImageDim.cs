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

namespace FitsRatingTool.Common.Models.FitsImage
{
    public readonly struct FitsImageDim : IFitsImageDim
    {
        private readonly int nx;
        private readonly int ny;
        private readonly int nc;
        private readonly int n;

        public FitsImageDim(int nx, int ny, int nc, int n)
        {
            this.nx = nx;
            this.ny = ny;
            this.nc = nc;
            this.n = n;
        }

        public int Width => nx;

        public int Height => ny;

        public int Channels => nc;

        public int NumElements => n;
    }
}
