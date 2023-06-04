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
    public static class IGenericFactoryExtensions
    {
        public static Instance Instantiate<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> container, out IDisposable disposable)
            where Instance : class
        {
            return factory.Instantiate(container.Instantiate, instance => container.Destroy(instance), out disposable);
        }

        public static Instance Instantiate<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> container, ILifecyclePublisher owner)
            where Instance : class
        {
            var instance = factory.Instantiate(container.Instantiate, instance => container.Destroy(instance), out var disposable);
            void onDestroyed()
            {
                owner.OnDestroyed -= onDestroyed;
                disposable.Dispose();
            }
            owner.OnDestroyed += onDestroyed;
            return instance;
        }

        public static IDisposable Instantiate<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, IContainer<Instance, Parameter> container, out Instance instance)
            where Instance : class
        {
            return factory.Instantiate(container.Instantiate, instance => container.Destroy(instance), out instance);
        }

        public static IDisposable Instantiate<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, Func<Parameter, Instance> instanceConstructor, Action<Instance> instanceDestructor, out Instance instance)
            where Instance : class
        {
            instance = factory.Instantiate(instanceConstructor, instanceDestructor, out var disposable);
            return disposable;
        }

        public static Instance Instantiate<Instance, Parameter>(this IGenericFactory<Instance, Parameter> factory, Func<Parameter, Instance> instanceConstructor, Action<Instance> instanceDestructor, ILifecyclePublisher owner)
            where Instance : class
        {
            var instance = factory.Instantiate(instanceConstructor, instanceDestructor, out var disposable);
            void onDestroyed()
            {
                owner.OnDestroyed -= onDestroyed;
                disposable.Dispose();
            }
            owner.OnDestroyed += onDestroyed;
            return instance;
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
    }
}
