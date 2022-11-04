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

using System.Reflection;

namespace FitsRatingTool.IoC
{
    public interface IContainerResolver
    {
        public interface IScope : IDisposable
        {
            /// <summary>
            /// Parent <see cref="IContainerResolver"/>, i.e., the <see cref="IContainerResolver"/> from which this scope was created via <see cref="OpenScope(object?)"/>.
            /// </summary>
            IContainerResolver Resolver { get; }

            /// <summary>
            /// A unique key which identifies this scope. The returned value must not change. Used by <seealso cref="IContainerResolver.RegisterInitializer(Action{IContainerLifecycle, IScope}, Predicate{object})"/>
            /// to determine whether an injected object was resolved through this <see cref="IScope"/> and <see cref="Resolver"/>.
            /// </summary>
            object Key { get; }

            /// <summary>
            /// Resolves the specified <typeparamref name="Service"/> for the given <typeparamref name="Parameter"/>.
            /// The implementation of this method must inject itself (<see cref="IScope"/>) and its <see cref="Resolver"/> such that when
            /// a service resolves <see cref="IScope"/> or <see cref="IContainerResolver"/> it'll obtain this object or <see cref="Resolver"/>.
            /// </summary>
            /// <typeparam name="Service"></typeparam>
            /// <typeparam name="Parameter"></typeparam>
            /// <param name="parameter"></param>
            /// <returns></returns>
            Service Resolve<Service, Parameter>(Parameter parameter);
        }

        /// <summary>
        /// Opens a new resolution scope.
        /// </summary>
        /// <param name="scopeName"></param>
        /// <returns></returns>
        IScope OpenScope(object? scopeName);

        /// <summary>
        /// Returns a scope name that is unique and consistent per class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        object GetClassScopeName<T>();

        /// <summary>
        /// Registers a <typeparamref name="Implementation"/> for the specified <typeparamref name="Service"/>.
        /// </summary>
        /// <typeparam name="Service"></typeparam>
        /// <typeparam name="Implementation"></typeparam>
        /// <param name="ctor">Constructor through which the <typeparamref name="Implementation"/> must be created. If <see langword="null"/>, the implementation of this method may decide which constructor to use.</param>
        void RegisterService<Service, Implementation>(ConstructorInfo? ctor = null) where Implementation : Service;

        /// <summary>
        /// Registers an initializer. The implementation must invoke <paramref name="initializer"/> when an <see cref="IContainer{Instance, Parameter}"/> that also implements <see cref="IContainerLifecycle"/>
        /// is injected and was resolved through this <see cref="IContainerResolver"/> and not a child <see cref="IContainerResolver"/>. The implementation may use <paramref name="scopeKeyPredicate"/>
        /// to check whether an injected object was resolved through this <see cref="IContainerResolver"/>, by checking whether a scope key (<seealso cref="IScope.Key"/>)
        /// belongs to this container, in which case <paramref name="scopeKeyPredicate"/> will return <see langword="true"/>.
        /// </summary>
        /// <param name="initializer"></param>
        /// <param name="scopeKeyPredicate"></param>
        void RegisterInitializer(Action<IContainerLifecycle, IScope> initializer, Predicate<object> scopeKeyPredicate);

        /// <summary>
        /// Creates a child <see cref="IContainerResolver"/> for which new registrations will not affect the parent <see cref="IContainerResolver"/>.
        /// Registrations or instances of the parent <see cref="IContainerResolver"/> must be inherited by the child <see cref="IContainerResolver"/>.
        /// </summary>
        /// <returns></returns>
        IContainerResolver CreateChild();
    }
}
