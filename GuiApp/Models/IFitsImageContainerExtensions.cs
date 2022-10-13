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
using FitsRatingTool.GuiApp.Services;
using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.Models
{
    public static class IFitsImageContainerExtensions
    {
        public static IFitsImage? FindImage(this IFitsImageManager.IRecord record, string file, Predicate<IFitsImage>? predicate = null)
        {
            foreach (var container in record.ImageContainers)
            {
                var image = FindImage(container, file, predicate);
                if (image != null)
                {
                    return image;
                }
            }
            return null;
        }

        public static IFitsImage? FindImage(this IFitsImageManager.IRecord record, Predicate<IFitsImage> predicate = null)
        {
            foreach (var container in record.ImageContainers)
            {
                var image = FindImage(container, predicate);
                if (image != null)
                {
                    return image;
                }
            }
            return null;
        }

        public static IFitsImage? FindImage(this IFitsImageContainer container, string file, Predicate<IFitsImage>? predicate = null)
        {
            return FindImage(container, i => i.File == file && (predicate == null || predicate.Invoke(i)));
        }

        public static IFitsImage? FindImage(this IFitsImageContainer container, Predicate<IFitsImage> predicate)
        {
            foreach (var image in container.FitsImages)
            {
                if (predicate.Invoke(image))
                {
                    return image;
                }
            }
            return null;
        }
    }
}
