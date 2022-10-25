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
        public static ITemplatedInstantiator<T, Template> Templated<T, Template>(this IInstantiatorFactory<T, Template> factory, Template template)
            where T : class
        {
            return factory.Templated(() => template);
        }

        public static IDelegatedInstantiator<T, Template> Delegated<T, Template>(this IInstantiatorFactory<T, Template> factory, Template template, Func<Template, T?> instanceConstructor, Action<T> instanceDestructor)
            where T : class
        {
            return factory.Delegated(() => template, instanceConstructor, instanceDestructor);
        }

        public static IDelegatedInstantiator<T, Template> Delegated<T, Template>(this IInstantiatorFactory<T, Template> factory, Func<Template?> templateConstructor, IContainer<T, Template> container)
            where T : class
        {
            return factory.Delegated(templateConstructor, container.Instantiate, instance => container.Destroy(instance));
        }

        public static IDelegatedInstantiator<T, Template> Delegated<T, Template>(this IInstantiatorFactory<T, Template> factory, Template template, IContainer<T, Template> container)
            where T : class
        {
            return factory.Delegated(() => template, container);
        }

        public static T Instantiate<T, Template>(this ITemplatedInstantiator<T, Template> instantiator, IContainer<T, Template> container)
            where T : class
        {
            return instantiator.Instantiate(container.Instantiate);
        }

        public static T Instantiate<T, Template>(this IGenericInstantiator<T, Template> instantiator, IContainer<T, Template> container, out IDisposable disposable)
            where T : class
        {
            return instantiator.Instantiate(container.Instantiate, instance => container.Destroy(instance), out disposable);
        }

        public static void Do<T, Template>(this IGenericInstantiator<T, Template> instantiator, IContainer<T, Template> temporaryContainer, Action<T> action)
            where T : class
        {
            var instance = instantiator.Instantiate(temporaryContainer, out var disposable);
            try
            {
                action.Invoke(instance);
            }
            finally
            {
                disposable.Dispose();
            }
        }

        public static async Task DoAsync<T, Template>(this IGenericInstantiator<T, Template> instantiator, IContainer<T, Template> temporaryContainer, Func<T, Task> action)
            where T : class
        {
            var instance = instantiator.Instantiate(temporaryContainer, out var disposable);
            try
            {
                await action.Invoke(instance);
            }
            finally
            {
                disposable.Dispose();
            }
        }

        public static void Do<T>(this IDelegatedInstantiator<T> instantiator, Action<T> action)
            where T : class
        {
            var instance = instantiator.Instantiate(out var disposable);
            try
            {
                action.Invoke(instance);
            }
            finally
            {
                disposable.Dispose();
            }
        }

        public static async Task DoAsync<T>(this IDelegatedInstantiator<T> instantiator, Func<T, Task> action)
            where T : class
        {
            var instance = instantiator.Instantiate(out var disposable);
            try
            {
                await action.Invoke(instance);
            }
            finally
            {
                disposable.Dispose();
            }
        }
    }
}
