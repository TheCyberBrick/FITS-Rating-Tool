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

using System.Reactive.Disposables;

namespace FitsRatingTool.IoC
{
    /// <summary>
    /// Register this registration to the IoC container to make it available to <see cref="IComponentRegistry{T}"/>'s.
    /// Other classes may inject such an <see cref="IComponentRegistry{T}"/> with the same <typeparamref name="Base"/> type to
    /// be able to access registered <see cref="IComponentRegistration{Base}"/>'s with the same <typeparamref name="Base"/> type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IComponentRegistration<Base>
        where Base : class
    {
        /// <summary>
        /// ID of this component. Must be unique per <typeparamref name="Base"/> type.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Name of this component. May be localized.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Base type of this registration.
        /// </summary>
        Type BaseType => typeof(Base);

        /// <summary>
        /// Creates a factory for creating instances of this registration's <typeparamref name="Base"/> type.
        /// </summary>
        /// <param name="disposable">This registration may use the specified <see cref="CompositeDisposable"/> to clean up the returned factory. The caller must dispose it when the factory is no longer needed.</param>
        /// <returns></returns>
        IDelegatedFactory<Base> CreateFactory(CompositeDisposable disposable);
    }
}
