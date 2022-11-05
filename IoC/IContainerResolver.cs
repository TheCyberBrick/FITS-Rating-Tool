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
            /// A unique key which identifies this scope. The returned value must not change. Used by <seealso cref="Fork{Service, Implementation}(ConstructorInfo?, Action{IContainerLifecycle, IScope}, Predicate{object})"/>
            /// to determine whether an injected object was resolved through this <see cref="IScope"/> and <see cref="Resolver"/>.
            /// </summary>
            object Key { get; }

            /// <summary>
            /// Resolves the specified <typeparamref name="Service"/> for the given <typeparamref name="Parameter"/>.
            /// The implementation of this method must inject itself (<see cref="IScope"/>) and its <see cref="Resolver"/> such that when
            /// a service resolves <see cref="IScope"/> or <see cref="IContainerResolver"/> it must obtain this object (<see cref="IScope"/>) respectively <see cref="Resolver"/> (<see cref="IContainerResolver"/>).
            /// </summary>
            /// <typeparam name="Service"></typeparam>
            /// <typeparam name="Parameter"></typeparam>
            /// <param name="parameter"></param>
            /// <returns></returns>
            Service Resolve<Service, Parameter>(Parameter parameter);
        }

        /// <summary>
        /// Opens a new resolution scope with an optionally specified scope name.
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
        /// <list type="number">
        /// <item>
        /// Creates and returns a child <see cref="IContainerResolver"/> for which new registrations will not affect the parent <see cref="IContainerResolver"/>s (e.g. this <see cref="IContainerResolver"/>).
        /// Registrations or instances of the parent (i.e. this) <see cref="IContainerResolver"/> must be inherited by the returned child <see cref="IContainerResolver"/>.
        /// </item>
        /// <item>
        /// Registers a <typeparamref name="Implementation"/> for the specified <typeparamref name="Service"/> to the child <see cref="IContainerResolver"/>.
        /// </item>
        /// <item>
        /// Registers an initializer to the child <see cref="IContainerResolver"/>. The implementation must invoke <paramref name="initializer"/> when an <see cref="IContainer{Instance, Parameter}"/> that also implements <see cref="IContainerLifecycle"/>
        /// is resolved through the returned <see cref="IContainerResolver"/> (and not a child <see cref="IContainerResolver"/> of it). The implementation may use <paramref name="scopeKeyPredicate"/>
        /// to check whether an object is being resolved through the returned <see cref="IContainerResolver"/>, by checking whether a certain scope key (see <seealso cref="IScope.Key"/>)
        /// belongs to a scope created from the returned container, in which case <paramref name="scopeKeyPredicate"/> will return <see langword="true"/>.
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="ctor">Constructor through which the <typeparamref name="Implementation"/> must be created/resolved. If <see langword="null"/>, the implementation of this method may freely decide which constructor to use.</param>
        /// <param name="initializer">Initializer callback to be called when an <see cref="IContainer{Instance, Parameter}"/> that also implements <see cref="IContainerLifecycle"/> is resolved through the returned <see cref="IContainerResolver"/> (and not a child <see cref="IContainerResolver"/> of it).</param>
        /// <param name="scopeKeyPredicate">Can be used to check whether an object is being resolved from a scope created from the returned container, in which case the predicate will return <see langword="true"/>. See <seealso cref="IScope.Key"/>.</param>
        /// <returns></returns>
        IContainerResolver Fork<Service, Implementation>(ConstructorInfo? ctor, Action<IContainerLifecycle, IScope> initializer, Predicate<object> scopeKeyPredicate) where Implementation : Service;

        /// <summary>
        /// Called when this <see cref="IContainerResolver"/> is no longer needed, but only if it was created through <see cref="Fork{Service, Implementation}(ConstructorInfo?, Action{IContainerLifecycle, IScope}, Predicate{object})"/>.
        /// </summary>
        void DestroyFork();
    }
}
