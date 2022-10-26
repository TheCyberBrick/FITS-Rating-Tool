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

using System;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IInstantiatorFactory<T, Template> : IDisposable
        where T : class
    {
        /// <summary>
        /// Creates an instantiator whose instance construction is done by the instantiator's consumer.
        /// The instances' lifetime is determined entirely by the consumer. The consumer is responsible
        /// for the deconstruction of the instances.
        /// </summary>
        /// <param name="templateConstructor"></param>
        /// <param name="allowMultipleInstantiations"></param>
        /// <returns></returns>
        ITemplatedInstantiator<T, Template> Templated(Func<Template?> templateConstructor, bool allowMultipleInstantiations = false);

        /// <summary>
        /// Creates an instantiator whose instance construction and destruction is delegated to this factory itself.
        /// The instances' lifetime is at most the factory's lifetime. When the factory is disposed, all the instances
        /// it created are deconstructed. Hence, the factory is responsible for the deconstruction of the created instances by disposing itself.
        /// However, the consumer of the instantiator may and should also trigger the deconstruction of the instances on its own when they're no longer needed.
        /// </summary>
        /// <param name="templateConstructor"></param>
        /// <param name="instanceConstructor"></param>
        /// <param name="instanceDestructor"></param>
        /// <param name="allowMultipleInstantiations"></param>
        /// <returns></returns>
        IDelegatedInstantiator<T, Template> Delegated(Func<Template?> templateConstructor, Func<Template, T?> instanceConstructor, Action<T> instanceDestructor, bool allowMultipleInstantiations = false);
    }

    public interface IInstantiatorBase<out T>
    {
        IInstantiatorBase<T> AndThen(Action<T> action);
    }

    public interface IGenericInstantiator<T, Template> : IInstantiatorBase<T>
    {
        new IGenericInstantiator<T, Template> AndThen(Action<T> action);

        /// <summary>
        /// Creates a new instance. The <paramref name="instanceConstructor"/> may or may not be used to construct the instance. However, if <paramref name="instanceConstructor"/> is used to
        /// construct the instance, then <paramref name="instanceDestructor"/> will be called exactly once when <paramref name="disposable"/> is disposed and/or the owning factory is disposed.
        /// The caller is responsible for the deconstruction of the created instance by disposing <paramref name="disposable"/> when it is no longer needed.
        /// </summary>
        /// <param name="instanceConstructor"></param>
        /// <param name="instanceDestructor"></param>
        /// <param name="disposable"></param>
        /// <returns></returns>
        T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable);
    }

    public interface ITemplatedInstantiator<T, Template> : IGenericInstantiator<T, Template>
        where T : class
    {
        new ITemplatedInstantiator<T, Template> AndThen(Action<T> action);

        /// <summary>
        /// Creates a new instance through the specified constructor. The caller
        /// is responsible for the deconstruction of the created instance, if necessary.
        /// </summary>
        /// <param name="instanceConstructor"></param>
        /// <returns></returns>
        T Instantiate(Func<Template, T> instanceConstructor);
    }

    public interface IDelegatedInstantiator<out T> : IInstantiatorBase<T>
        where T : class
    {
        new IDelegatedInstantiator<T> AndThen(Action<T> action);

        /// <summary>
        /// Creates a new instance. The caller should deconstruct the created instance by disposing <paramref name="disposable"/>
        /// when it is no longer needed, otherwise the instance will stay around until the owning factory is disposed.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        T Instantiate(out IDisposable disposable);
    }

    public interface IDelegatedInstantiator<T, Template> : IGenericInstantiator<T, Template>, IDelegatedInstantiator<T>
        where T : class
    {
        new IDelegatedInstantiator<T, Template> AndThen(Action<T> action);
    }
}
