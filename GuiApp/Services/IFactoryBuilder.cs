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
    public interface IFactoryBuilder<T, Template> : IDisposable
        where T : class
    {
        /// <summary>
        /// Creates a factory whose instance construction is done by the factory's consumer.
        /// The instances' lifetime is determined entirely by the consumer. The consumer is responsible
        /// for the deconstruction of the instances.
        /// </summary>
        /// <param name="templateConstructor">Function that returns the template to construct the instance from. May return <see langword="null"/> to dispose the factory.</param>
        /// <param name="isSingleUse">Whether the factory should be disposed after the first instantiation.</param>
        /// <returns></returns>
        ITemplatedFactory<T, Template> Templated(Func<Template?> templateConstructor, bool isSingleUse = true);

        /// <summary>
        /// Creates a factory whose instance construction and destruction is delegated to the caller of this method.
        /// The created instances' lifetime is at most this builder's lifetime. When the builder is disposed, all the factories it created are disposed and all the
        /// instances it created are deconstructed. Hence, the caller of this method is responsible for the deconstruction of the created instances by disposing this factory builder.
        /// However, the consumer of the factory may and should also trigger the deconstruction of the instances on its own when they're no longer needed.
        /// </summary>
        /// <param name="templateConstructor">Function that returns the template to construct the instance from. May return <see langword="null"/> to dispose the factory.</param>
        /// <param name="instanceConstructor">Function that constructs the instance from a template. May return <see langword="null"/> to dispose the factory.</param>
        /// <param name="instanceDestructor">Function that deconstructs an instance created from the factory. Called exactly once for each created instance.</param>
        /// <param name="isSingleUse">Whether the factory should be disposed after the first instantiation.</param>
        /// <returns></returns>
        IDelegatedFactory<T, Template> Delegated(Func<Template?> templateConstructor, Func<Template, T?> instanceConstructor, Action<T> instanceDestructor, bool isSingleUse = true);
    }

    public interface IFactoryBase<out T>
    {
        bool IsSingleUse { get; }

        /// <summary>
        /// Returns a child factory that invokes the specified <paramref name="action"/>
        /// upon instantiation.
        /// </summary>
        /// <param name="action">Action to invoke upon instantiation.</param>
        /// <returns></returns>
        IFactoryBase<T> AndThen(Action<T> action);
    }

    public interface IGenericFactory<T, Template> : IFactoryBase<T>
    {
        /// <inheritdoc cref="IFactoryBase{T}.AndThen(Action{T})"/>
        new IGenericFactory<T, Template> AndThen(Action<T> action);

        /// <summary>
        /// Creates a new instance. The <paramref name="instanceConstructor"/> may or may not be used to construct the instance. However, if <paramref name="instanceConstructor"/> is used to
        /// construct the instance, then <paramref name="instanceDestructor"/> will be called exactly once when <paramref name="disposable"/> is disposed and/or the owning factory builder is disposed.
        /// The caller is responsible for the deconstruction of the created instance by disposing <paramref name="disposable"/> when it is no longer needed.
        /// </summary>
        /// <param name="instanceConstructor"></param>
        /// <param name="instanceDestructor"></param>
        /// <param name="disposable"></param>
        /// <returns></returns>
        T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable);
    }

    public interface ITemplatedFactory<T, Template> : IGenericFactory<T, Template>
        where T : class
    {
        /// <inheritdoc cref="IFactoryBase{T}.AndThen(Action{T})"/>
        new ITemplatedFactory<T, Template> AndThen(Action<T> action);

        /// <summary>
        /// Creates a new instance through the specified constructor. The caller
        /// is responsible for the deconstruction of the created instance, if necessary.
        /// </summary>
        /// <param name="instanceConstructor"></param>
        /// <returns></returns>
        T Instantiate(Func<Template, T> instanceConstructor);
    }

    public interface IDelegatedFactory<out T> : IFactoryBase<T>
        where T : class
    {
        /// <inheritdoc cref="IFactoryBase{T}.AndThen(Action{T})"/>
        new IDelegatedFactory<T> AndThen(Action<T> action);

        /// <summary>
        /// Creates a new instance. The caller should deconstruct the created instance by disposing <paramref name="disposable"/>
        /// when it is no longer needed, otherwise the instance will stay around until the owning factory builder is disposed.
        /// </summary>
        /// <param name="disposable">Object to dispose when the instance is no longer needed.</param>
        /// <returns></returns>
        T Instantiate(out IDisposable disposable);
    }

    public interface IDelegatedFactory<T, Template> : IGenericFactory<T, Template>, IDelegatedFactory<T>
        where T : class
    {
        /// <inheritdoc cref="IFactoryBase{T}.AndThen(Action{T})"/>
        new IDelegatedFactory<T, Template> AndThen(Action<T> action);
    }
}
