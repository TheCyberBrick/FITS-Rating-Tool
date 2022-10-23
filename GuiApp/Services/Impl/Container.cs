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

using DryIoc;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class Container<T, Template> : ReactiveObject, IContainer<T, Template>, IContainerLifecycle, IObservable<T?>
        where T : class
    {
        private IContainerLifecycle? parent;
        private object? dependee;

        private bool _isSingleton;
        public bool IsSingleton
        {
            get => _isSingleton;
            private set => this.RaiseAndSetIfChanged(ref _isSingleton, value);
        }

        public int Count => instance2Scope.Count;


        private bool _initialized;

        public bool IsInitialized
        {
            get => _initialized;
            private set => this.RaiseAndSetIfChanged(ref _initialized, value);
        }

        public IObservable<T?> WhenAny => this;

        private Action? _onInitialized;
        public event Action OnInitialized
        {
            add => _onInitialized += value;
            remove => _onInitialized -= value;
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;


        private readonly Dictionary<IResolverContext, T> scope2Instance = new();
        private readonly Dictionary<T, IResolverContext> instance2Scope = new();
        private readonly ConcurrentDictionary<IResolverContext, List<IContainerLifecycle>> scope2Dependencies = new();
        private readonly HashSet<IResolverContext> scopes = new();

        private readonly List<IObserver<T?>> observers = new();

        private bool disposed;

        private readonly DryIoc.IContainer container;
        private readonly object? scopeName;

        public Container(DryIoc.IContainer container, Func<IRegistrar<T, Template>, T> regCtor)
        {
            // Create child container so that the T registration
            // can be replaced/shadowed
            this.container = container = container.With(
                container,
                container.Rules.WithDefaultIfAlreadyRegistered(IfAlreadyRegistered.Replace),
                container.ScopeContext,
                RegistrySharing.CloneButKeepCache,
                container.SingletonScope,
                container.CurrentScope,
                IsRegistryChangePermitted.Permitted);

            // Register initializer to track injected dependencies
            container.Register<object>(
                made: Made.Of(
                    req => typeof(DependencyTracker)
                        .SingleMethod(nameof(DependencyTracker.OnDependencyInjected))
                        .MakeGenericMethod(req.ServiceType.GetGenericArguments()),
                    parameters: Parameters.Of.Type(req => (Action<IContainerLifecycle, IResolverContext>)OnDependencyInjected)),
                setup: Setup.DecoratorWith(
                    r =>
                    {
                        lock (this)
                        {
                            return scopes.Contains(r.Container) &&
                                r.FactoryType != FactoryType.Wrapper &&
                                r.ServiceType.IsGenericType &&
                                typeof(IContainer<,>).IsAssignableFrom(r.ServiceType.GetGenericTypeDefinition());
                        }
                    },
                    useDecorateeReuse: true,
                    preventDisposal: true)
                );

            // Invoke factory constructor to complete registration
            RegistrationCompletion? completion = null;
            try
            {
                regCtor.Invoke(new Registrar(container));
            }
            catch (RegistrationCompletion c)
            {
                completion = c;
            }

            // Make sure registration was completed or else
            // throw an error
            if (completion == null)
            {
                throw new InvalidOperationException($"Constructor did not call {typeof(IRegistrar<,>).Name}.{nameof(Registrar.RegisterAndReturn)}");
            }

            scopeName = completion.ScopeName;
        }

        private void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(typeof(IContainer<,>).Name, "Container is already disposed");
            }
        }

        public IContainer<T, Template> ToSingleton()
        {
            ChangeToSingleton();
            return this;
        }

        public IObservable<T?> ToSingletonWithObservable()
        {
            ChangeToSingleton();
            return this;
        }

        private void ChangeToSingleton()
        {
            lock (this)
            {
                CheckDisposed();

                if (!IsSingleton)
                {
                    if (IsInitialized)
                    {
                        throw new InvalidOperationException("Cannot change container to singleton after initialization");
                    }

                    IsSingleton = true;
                }
            }
        }

        private void OnDependencyInjected(IContainerLifecycle lifecycle, IResolverContext scope)
        {
            lock (this)
            {
                CheckDisposed();

                if (!scopes.Contains(scope))
                {
                    throw new InvalidOperationException("Dependency was injected with unknown scope");
                }

                scope2Dependencies.GetOrAdd(scope, s => new()).Add(lifecycle);

                if (scope2Instance.TryGetValue(scope, out var instance))
                {
                    // Dependency was created after the
                    // instance has already been constructed
                    // -> initialize injected dependency now
                    lifecycle.Initialize(this, instance);
                }
            }
        }

        void IContainerLifecycle.Initialize(IContainerLifecycle? parent, object? dependee)
        {
            lock (this)
            {
                CheckDisposed();

                this.parent = parent;
                this.dependee = dependee;

                IsInitialized = true;

                _onInitialized?.Invoke();
            }
        }

        private static void NotifyEventListenersOnAdded(object dependee, object dependency)
        {
            // Notify dependency about being added to dependee
            (dependency as IContainerRelations)?.OnAddedTo(dependee);

            // Notify dependee about added dependency
            (dependee as IContainerRelations)?.OnAdded(dependency);
        }

        public T Instantiate(Template template)
        {
            lock (this)
            {
                CheckDisposed();

                if (!IsInitialized)
                {
                    throw new InvalidOperationException($"Cannot instantiate before container ({typeof(T).FullName}, {typeof(Template).FullName}) is initialized");
                }
                else
                {
                    if (IsSingleton)
                    {
                        Destroy();
                    }

                    // Open a new scope with the scope previously
                    // passed by T to the registrar
                    var newScope = container.OpenScope(scopeName);
                    scopes.Add(newScope);

                    T? newInstance = null;
                    try
                    {
                        // Create a new instance
                        newInstance = newScope.Resolve<Func<Template, T>>().Invoke(template);

                        // Initialize injected dependencies
                        foreach (var dependencies in scope2Dependencies.Values)
                        {
                            foreach (var dependency in dependencies)
                            {
                                dependency.Initialize(this, newInstance);
                            }
                        }

                        this.RaisePropertyChanging(nameof(Count));

                        scope2Instance.Add(newScope, newInstance);
                        instance2Scope.Add(newInstance, newScope);

                        this.RaisePropertyChanged(nameof(Count));

                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newInstance));

                        if (dependee != null)
                        {
                            NotifyEventListenersOnAdded(dependee: dependee, dependency: newInstance);
                        }

                        // Notify observers about added dependency
                        foreach (var observer in observers)
                        {
                            observer.OnNext(newInstance);
                        }

                        // Notify instance about completed instantiation
                        (newInstance as IContainerInstantiation)?.OnInstantiated();

                        return newInstance;
                    }
                    catch (Exception ex)
                    {
                        if (newInstance != null)
                        {
                            Destroy(newInstance);

                            if (scopes.Remove(newScope))
                            {
                                newScope.Dispose();
                            }
                        }

                        foreach (var observer in observers)
                        {
                            observer.OnError(ex);
                        }

                        throw;
                    }
                }
            }
        }

        public bool Destroy(T instance)
        {
            lock (this)
            {
                if (disposed)
                {
                    // Nothing to do
                    return false;
                }

                if (instance2Scope.TryGetValue(instance, out var scope))
                {
                    var dependencies = scope2Dependencies.TryRemove(scope, out var v) ? v : Enumerable.Empty<IContainerLifecycle>();
                    foreach (var dependency in dependencies)
                    {
                        dependency.Destroy(true);
                    }

                    this.RaisePropertyChanging(nameof(Count));

                    scope2Instance.Remove(scope);
                    instance2Scope.Remove(instance);

                    this.RaisePropertyChanged(nameof(Count));

                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, instance));

                    // Notify parent's instance about removed dependency
                    (dependee as IContainerRelations)?.OnRemoved(instance);

                    // Notify observers about removed dependency
                    foreach (var observer in observers)
                    {
                        observer.OnNext(null);
                    }

                    // Dispose scopes which will also dispose
                    // the created instances
                    scope.Dispose();
                    scopes.Remove(scope);

                    return true;
                }
            }

            return false;
        }

        public void Destroy()
        {
            ((IContainerLifecycle)this).Destroy(false);
        }

        void IContainerLifecycle.Destroy(bool dispose)
        {
            lock (this)
            {
                if (disposed)
                {
                    // Nothing to do
                    return;
                }

                // Prevent initialization of new dependencies
                // if the container is being disposed
                if (dispose)
                {
                    disposed = true;
                }

                // Clear and dispose all dependencies first
                foreach (var dependencies in scope2Dependencies.Values)
                {
                    foreach (var dependency in dependencies)
                    {
                        dependency.Destroy(true);
                    }
                }
                scope2Dependencies.Clear();

                var oldInstances = new List<T>(scope2Instance.Values);

                this.RaisePropertyChanging(nameof(Count));

                scope2Instance.Clear();
                instance2Scope.Clear();

                this.RaisePropertyChanged(nameof(Count));

                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

                foreach (var instance in oldInstances)
                {
                    // Notify parent's instance about removed dependency
                    (dependee as IContainerRelations)?.OnRemoved(instance);

                    // Notify observers about removed dependency
                    foreach (var observer in observers)
                    {
                        observer.OnNext(null);
                    }
                }

                // Dispose scopes which will also dispose
                // the created instances
                foreach (var scope in scopes)
                {
                    scope?.Dispose();
                }
                scopes.Clear();

                if (dispose)
                {
                    foreach (var observer in observers)
                    {
                        observer.OnCompleted();
                    }
                    observers.Clear();

                    // Child container is not disposed because
                    // disposal of services is already handled
                    // by the scope, and disposing the child
                    // container would cause unintentional
                    // disposal of other services due to the
                    // container's scopes being shared instead
                    // of cloned
                }
            }
        }

        IDisposable IObservable<T?>.Subscribe(IObserver<T?> observer)
        {
            lock (this)
            {
                CheckDisposed();
                observers.Add(observer);
                observer.OnNext(this.FirstOrDefault());
            }
            return Disposable.Create(() =>
            {
                lock (this)
                {
                    observers.Remove(observer);
                }
            });
        }

        public IEnumerator<T> GetEnumerator()
        {
            return instance2Scope.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)instance2Scope.Keys).GetEnumerator();
        }

        private class Registrar : IRegistrar<T, Template>
        {
            private readonly DryIoc.IContainer container;

            public Registrar(DryIoc.IContainer container)
            {
                this.container = container;
            }

            public object ClassScopeName => ResolutionScopeName.Of<T>();

            [DoesNotReturn]
            public void RegisterAndReturn<TImpl>(object? scopeName = null, ConstructorInfo? constructor = null)
                where TImpl : class, T
            {
                // Register implementation with appropriate reuse and factory

                if (constructor != null)
                {
                    if (constructor.DeclaringType != typeof(TImpl))
                    {
                        throw new InvalidOperationException($"Constructor is not declared by {typeof(TImpl).FullName}");
                    }

                    container.Register<T, TImpl>(reuse: Reuse.Scoped, made: Made.Of(constructor));
                }
                else
                {
                    ConstructorInfo? instantiatorCtor = null;

                    foreach (var ctor in typeof(TImpl).GetTypeInfo().DeclaredConstructors)
                    {
                        if (ctor.GetCustomAttribute<InstantiatorAttribute>() != null)
                        {
                            if (instantiatorCtor != null)
                            {
                                throw new InvalidOperationException($"Multiple instantiator constructors in {typeof(TImpl).FullName}");
                            }
                            instantiatorCtor = ctor;
                        }
                    }

                    if (instantiatorCtor != null)
                    {
                        container.Register<T, TImpl>(reuse: Reuse.Scoped, made: Made.Of(instantiatorCtor));
                    }
                    else
                    {
                        container.Register<T, TImpl>(reuse: Reuse.Scoped, made: Made.Of(FactoryMethod.Constructor(mostResolvable: true, includeNonPublic: true)));
                    }
                }

                throw new RegistrationCompletion(typeof(TImpl), scopeName);
            }
        }

        private class RegistrationCompletion : Exception
        {
            public Type RegisteredType { get; }

            public object? ScopeName { get; }

            public RegistrationCompletion(Type registeredType, object? scopeName)
            {
                RegisteredType = registeredType;
                ScopeName = scopeName;
            }
        }

        private static class DependencyTracker
        {
            public static IContainer<TT, TTemplate> OnDependencyInjected<TT, TTemplate>(IContainer<TT, TTemplate> container, IResolverContext scope, Action<IContainerLifecycle, IResolverContext> action)
                where TT : class
            {
                if (container is IContainerLifecycle lifecycle)
                {
                    action.Invoke(lifecycle, scope);
                }
                return container;
            }
        }
    }
}
