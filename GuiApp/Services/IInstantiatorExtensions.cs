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

using FitsRatingTool.GuiApp.Services;
using System;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Services
{
    public static class IInstantiatorExtensions
    {
        public static T Instantiate<T, Template>(this IInstantiator<T, Template> instantiator, IContainer<T, Template> container)
            where T : class
        {
            return instantiator.Instantiate(container.Instantiate);
        }

        public static void Do<T, Template>(this IInstantiator<T, Template> instantiator, IContainer<T, Template> temporaryContainer, Action<T> action)
            where T : class
        {
            var instance = instantiator.Instantiate(temporaryContainer);
            try
            {
                action.Invoke(instance);
            }
            finally
            {
                temporaryContainer.Destroy(instance);
            }
        }

        public static async Task DoAsync<T, Template>(this IInstantiator<T, Template> instantiator, IContainer<T, Template> temporaryContainer, Func<T, Task> action)
            where T : class
        {
            var instance = instantiator.Instantiate(temporaryContainer);
            try
            {
                await action.Invoke(instance);
            }
            finally
            {
                temporaryContainer.Destroy(instance);
            }
        }
    }
}
