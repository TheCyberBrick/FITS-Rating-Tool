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

using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.FitsLoader.Native;

namespace FitsRatingTool.Common.Services.Impl
{
    public class FitsImageLoader : IFitsImageLoader
    {
        private readonly INativeFitsLoader loader;

        public FitsImageLoader()
        {
            loader = NativeFitsLoaderFactory.Create();
        }

        public IFitsImage? LoadFit(string file, long maxInputSize, int maxWidth, int maxHeight)
        {
            var fitsHandle = loader.LoadFit(file, maxInputSize, maxWidth, maxHeight);

            if (fitsHandle.Valid == 1)
            {
                return new NativeFitsImage(loader, file, fitsHandle);
            }

            return null;
        }
    }
}
