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
using FitsRatingTool.Common.Services;
using FitsRatingTool.Common.Services.Impl;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class AppFitsImageLoader : IFitsImageLoader
    {
        private readonly IAppConfig appConfig;

        private readonly FitsImageLoader defaultLoader = new();

        public AppFitsImageLoader(IAppConfig appConfig)
        {
            this.appConfig = appConfig;
        }

        public IFitsImage? LoadFit(string file, long maxInputSize, int maxWidth, int maxHeight)
        {
            var image = defaultLoader.LoadFit(file, maxInputSize, maxWidth, maxHeight);

            if (image != null)
            {
                image.AlwaysUnloadImageData = !appConfig.KeepImageDataLoaded;
            }

            return image;
        }
    }
}
