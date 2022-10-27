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
    public static class IFactoryBuilderExtensions
    {
        public static ITemplatedFactory<T, Template> Templated<T, Template>(this IFactoryBuilder<T, Template> factory, Template template, bool isSingleUse = true)
            where T : class
        {
            return factory.Templated(() => template, isSingleUse);
        }

        public static IDelegatedFactory<T, Template> Delegated<T, Template>(this IFactoryBuilder<T, Template> factory, Template template, Func<Template, T?> instanceConstructor, Action<T> instanceDestructor, bool isSingleUse = true)
            where T : class
        {
            return factory.Delegated(() => template, instanceConstructor, instanceDestructor, isSingleUse);
        }

        public static IDelegatedFactory<T, Template> Delegated<T, Template>(this IFactoryBuilder<T, Template> factory, Func<Template?> templateConstructor, IContainer<T, Template> container, bool isSingleUse = true)
            where T : class
        {
            return factory.Delegated(templateConstructor, container.Instantiate, instance => container.Destroy(instance), isSingleUse);
        }

        public static IDelegatedFactory<T, Template> Delegated<T, Template>(this IFactoryBuilder<T, Template> factory, Template template, IContainer<T, Template> container, bool isSingleUse = true)
            where T : class
        {
            return factory.Delegated(() => template, container, isSingleUse);
        }

        public static T Instantiate<T, Template>(this ITemplatedFactory<T, Template> factory, IContainer<T, Template> container)
            where T : class
        {
            return factory.Instantiate(container.Instantiate);
        }

        public static T Instantiate<T, Template>(this IGenericFactory<T, Template> factory, IContainer<T, Template> container, out IDisposable disposable)
            where T : class
        {
            return factory.Instantiate(container.Instantiate, instance => container.Destroy(instance), out disposable);
        }

        public static void Do<T, Template>(this IGenericFactory<T, Template> factory, IContainer<T, Template> temporaryContainer, Action<T> action)
            where T : class
        {
            var instance = factory.Instantiate(temporaryContainer, out var disposable);
            try
            {
                action.Invoke(instance);
            }
            finally
            {
                disposable.Dispose();
            }
        }

        public static async Task DoAsync<T, Template>(this IGenericFactory<T, Template> factory, IContainer<T, Template> temporaryContainer, Func<T, Task> action)
            where T : class
        {
            var instance = factory.Instantiate(temporaryContainer, out var disposable);
            try
            {
                await action.Invoke(instance);
            }
            finally
            {
                disposable.Dispose();
            }
        }

        public static void Do<T>(this IDelegatedFactory<T> factory, Action<T> action)
            where T : class
        {
            var instance = factory.Instantiate(out var disposable);
            try
            {
                action.Invoke(instance);
            }
            finally
            {
                disposable.Dispose();
            }
        }

        public static async Task DoAsync<T>(this IDelegatedFactory<T> factory, Func<T, Task> action)
            where T : class
        {
            var instance = factory.Instantiate(out var disposable);
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
