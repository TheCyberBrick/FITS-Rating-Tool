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

namespace FitsRatingTool.IoC
{
    public static class IFactoryRootExtensions
    {
        public static IParameterizedFactory<Instance, Parameter> Parameterized<Instance, Parameter>(this IFactoryRoot<Instance, Parameter> factory, Parameter parameter, bool isSingleUse = true)
            where Instance : class
        {
            return factory.Parameterized(() => parameter, isSingleUse);
        }

        public static IDelegatedFactory<Instance, Parameter> Delegated<Instance, Parameter>(this IFactoryRoot<Instance, Parameter> factory, Parameter parameter, Func<Parameter, Instance?> instanceConstructor, Action<Instance> instanceDestructor, bool isSingleUse = true)
            where Instance : class
        {
            return factory.Delegated(() => parameter, instanceConstructor, instanceDestructor, isSingleUse);
        }

        public static IDelegatedFactory<Instance, Parameter> Delegated<Instance, Parameter>(this IFactoryRoot<Instance, Parameter> factory, Func<Parameter?> parameterConstructor, IContainer<Instance, Parameter> container, bool isSingleUse = true)
            where Instance : class
        {
            return factory.Delegated(parameterConstructor, container.Instantiate, instance => container.Destroy(instance), isSingleUse);
        }

        public static IDelegatedFactory<Instance, Parameter> Delegated<Instance, Parameter>(this IFactoryRoot<Instance, Parameter> factory, Parameter parameter, IContainer<Instance, Parameter> container, bool isSingleUse = true)
            where Instance : class
        {
            return factory.Delegated(() => parameter, container, isSingleUse);
        }

        public static Instance Instantiate<Instance, Parameter>(this IParameterizedFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> container)
            where Instance : class
        {
            return factory.Instantiate(container.Instantiate);
        }

        public static Instance Instantiate<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> container, out IDisposable disposable)
            where Instance : class
        {
            return factory.Instantiate(container.Instantiate, instance => container.Destroy(instance), out disposable);
        }

        public static IDisposable Instantiate<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> container, out Instance instance)
            where Instance : class
        {
            return factory.Instantiate(container.Instantiate, instance => container.Destroy(instance), out instance);
        }

        public static IDisposable Instantiate<Instance>(this IDelegatedFactory<Instance> factory, out Instance instance)
            where Instance : class
        {
            instance = factory.Instantiate(out var disposable);
            return disposable;
        }

        public static IDisposable Instantiate<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, Func<Parameter, Instance> instanceConstructor, Action<Instance> instanceDestructor, out Instance instance)
            where Instance : class
        {
            instance = factory.Instantiate(instanceConstructor, instanceDestructor, out var disposable);
            return disposable;
        }

        public static void Do<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> temporaryContainer, Action<Instance> action)
            where Instance : class
        {
            using (factory.Instantiate(temporaryContainer, out Instance instance))
            {
                action.Invoke(instance);
            }
        }

        public static R Do<Instance, Parameter, R>(this IGenericFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> temporaryContainer, Func<Instance, R> action)
            where Instance : class
        {
            using (factory.Instantiate(temporaryContainer, out Instance instance))
            {
                return action.Invoke(instance);
            }
        }

        public static async Task DoAsync<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> temporaryContainer, Func<Instance, Task> action)
            where Instance : class
        {
            using (factory.Instantiate(temporaryContainer, out Instance instance))
            {
                await action.Invoke(instance);
            }
        }

        public static async Task<R> DoAsync<Instance, Parameter, R>(this IGenericFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> temporaryContainer, Func<Instance, Task<R>> action)
            where Instance : class
        {
            using (factory.Instantiate(temporaryContainer, out Instance instance))
            {
                return await action.Invoke(instance);
            }
        }

        public static void Do<Instance>(this IDelegatedFactory<Instance> factory, Action<Instance> action)
            where Instance : class
        {
            using (factory.Instantiate(out Instance instance))
            {
                action.Invoke(instance);
            }
        }

        public static R Do<Instance, R>(this IDelegatedFactory<Instance> factory, Func<Instance, R> action)
            where Instance : class
        {
            using (factory.Instantiate(out Instance instance))
            {
                return action.Invoke(instance);
            }
        }

        public static async Task DoAsync<Instance>(this IDelegatedFactory<Instance> factory, Func<Instance, Task> action)
            where Instance : class
        {
            using (factory.Instantiate(out Instance instance))
            {
                await action.Invoke(instance);
            }
        }

        public static async Task<R> DoAsync<Instance, R>(this IDelegatedFactory<Instance> factory, Func<Instance, Task<R>> action)
            where Instance : class
        {
            using (factory.Instantiate(out Instance instance))
            {
                return await action.Invoke(instance);
            }
        }
    }
}
