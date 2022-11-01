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
using System.Reactive.Subjects;
using System.Reflection;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class Container<T, Template> : ReactiveObject, IContainer<T, Template>, IContainerLifecycle
        where T : class
    {
        private IContainerLifecycle? parent;
        private object? dependee;

        private bool isContainerSingleton;

        private bool _isSingleton;
        public bool IsSingleton
        {
            get => _isSingleton;
            private set => this.RaiseAndSetIfChanged(ref _isSingleton, value);
        }

        public int Count => instance2Scope.Count;


        private bool isContainerInitialized;

        private bool _initialized;
        public bool IsInitialized
        {
            get => _initialized;
            private set => this.RaiseAndSetIfChanged(ref _initialized, value);
        }

        private Action? _onInitialized;
        public event Action OnInitialized
        {
            add => _onInitialized += value;
            remove => _onInitialized -= value;
        }

        private Action<T>? _onInstantiated;
        public event Action<T> OnInstantiated
        {
            add => _onInstantiated += value;
            remove => _onInstantiated -= value;
        }

        private Action<T>? _onDestroyed;
        public event Action<T> OnDestroyed
        {
            add => _onDestroyed += value;
            remove => _onDestroyed -= value;
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;


        private readonly Dictionary<IResolverContext, T> scope2Instance = new();
        private readonly ConcurrentDictionary<T, IResolverContext> instance2Scope = new();

        private readonly ConcurrentDictionary<IResolverContext, List<IContainerLifecycle>> scope2Dependencies = new();
        private readonly HashSet<IResolverContext> scopes = new();


        private readonly ConcurrentDictionary<IResolverContext, List<IContainerLifecycle>> loadingScope2Dependencies = new();
        private readonly HashSet<IResolverContext> loadingScopes = new();

        private readonly HashSet<T> loadingInstances = new();

        private readonly ReplaySubject<T?> singletonSubject = new(bufferSize: 1);

        private object generationKey = new();

        private T? singletonInstance;

        private bool disposed;

        private int reentrancyCount;

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
                        .SingleMethod(nameof(DependencyTracker.CreateAndTrackDependency))
                        .MakeGenericMethod(req.ServiceType.GetGenericArguments()),
                    parameters: Parameters.Of.Type(req => (Action<IContainerLifecycle, IResolverContext>)OnDependencyInjected)),
                setup: Setup.DecoratorWith(
                    r =>
                    {
                        lock (this)
                        {
                            return (scopes.Contains(r.Container) || loadingScopes.Contains(r.Container)) &&
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

        private void CheckReentrancy()
        {
            if (reentrancyCount > 0)
            {
                throw new InvalidOperationException($"Illegal reentrant modification");
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
            return singletonSubject;
        }

        private void ChangeToSingleton()
        {
            CheckReentrancy();

            lock (this)
            {
                CheckDisposed();

                if (!isContainerSingleton)
                {
                    if (isContainerInitialized)
                    {
                        throw new InvalidOperationException("Cannot change container to singleton after initialization");
                    }

                    isContainerSingleton = true;
                }
            }

            IsSingleton = true;
        }

        private void OnDependencyInjected(IContainerLifecycle lifecycle, IResolverContext scope)
        {
            CheckReentrancy();

            T? injectedInstance = null;

            lock (this)
            {
                CheckDisposed();

                if (!scopes.Contains(scope) && !loadingScopes.Contains(scope))
                {
                    throw new InvalidOperationException("Dependency was injected with unknown scope");
                }

                loadingScope2Dependencies.GetOrAdd(scope, s => new()).Add(lifecycle);

                if (scope2Instance.TryGetValue(scope, out var instance))
                {
                    injectedInstance = instance;
                }
            }

            // Dependency was created after the
            // instance has already been constructed
            // -> initialize injected dependency now
            if (injectedInstance != null)
            {
                lifecycle.Initialize(this, injectedInstance);
            }
        }

        void IContainerLifecycle.Initialize(IContainerLifecycle? parent, object? dependee)
        {
            CheckReentrancy();

            lock (this)
            {
                CheckDisposed();

                this.parent = parent;
                this.dependee = dependee;

                isContainerInitialized = true;
            }

            IsInitialized = true;

            _onInitialized?.Invoke();
        }

        private static void NotifyEventListenersOnAdded(object dependee, object dependency)
        {
            // Notify dependency about being added to dependee
            (dependency as IContainerDependencyListener)?.OnAddedTo(dependee);

            // Notify dependee about added dependency
            (dependee as IContainerDependencyListener)?.OnAdded(dependency);
        }

        public T Instantiate(Template template)
        {
            CheckReentrancy();

            IResolverContext newScope;
            object? newGenerationKey;
            bool clearContainer = isContainerSingleton;

            lock (this)
            {
                CheckDisposed();

                if (!isContainerInitialized)
                {
                    throw new InvalidOperationException($"Cannot instantiate before container ({typeof(T).FullName}, {typeof(Template).FullName}) is initialized");
                }

                if (isContainerSingleton && singletonInstance == null)
                {
                    newGenerationKey = generationKey = new object();
                    clearContainer = false;
                }
                else
                {
                    newGenerationKey = generationKey;
                }
            }

            if (clearContainer)
            {
                newGenerationKey = DestroyInternal(false);
            }

            // Open a new scope with the scope name previously
            // passed by T to the registrar
            newScope = container.OpenScope(scopeName);

            void destroyInstanceAndScope(T? instance)
            {
                if (instance != null)
                {
                    Destroy(instance);

                    // If instance is destroyed due to exception
                    // in constructor then the scope needs to be
                    // disposed manually because the instance
                    // (== null) cannot be mapped to the scope
                    bool disposeScope;
                    lock (this)
                    {
                        disposeScope = scopes.Remove(newScope);
                    }
                    if (disposeScope)
                    {
                        newScope.Dispose();
                    }
                }
            }

            T? newInstance = null;
            try
            {
                lock (this)
                {
                    // Add new scope to loadingScopes so that
                    // when the container is cleared it won't
                    // destroy the new instance or dispose the
                    // new scope because Instantiate will take
                    // care of the destruction
                    loadingScopes.Add(newScope);
                }

                // Create a new instance.
                // Passes newScope explicitly to make sure that
                // OnDependencyInjected receives the same scope
                // instance so that it can track the dependencies
                // properly
                newInstance = newScope.Resolve<Func<Template, IResolverContext, T>>().Invoke(template, newScope);

                List<IContainerLifecycle>? dependencies = null;

                bool added = false;

                lock (this)
                {
                    CheckDisposed();

                    loadingScopes.Remove(newScope);
                    loadingInstances.Add(newInstance);

                    // Check if the container has been cleared
                    // since the new instance was created, and
                    // and if so, don't add the new instance and
                    // immediately destroy it again further down
                    if (generationKey == newGenerationKey)
                    {
                        added = true;

                        ++reentrancyCount;
                        try
                        {
                            this.RaisePropertyChanging(nameof(Count));

                            if (loadingScope2Dependencies.TryRemove(newScope, out dependencies))
                            {
                                scope2Dependencies.TryAdd(newScope, dependencies);
                            }

                            scopes.Add(newScope);
                            scope2Instance.Add(newScope, newInstance);
                            instance2Scope.TryAdd(newInstance, newScope);

                            this.RaisePropertyChanged(nameof(Count));

                            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newInstance));
                        }
                        finally
                        {
                            --reentrancyCount;
                        }

                        singletonInstance = newInstance;
                    }
                }

                if (added)
                {
                    // Initialize injected dependencies
                    if (dependencies != null)
                    {
                        foreach (var dependency in dependencies)
                        {
                            dependency.Initialize(this, newInstance);
                        }
                    }

                    if (dependee != null)
                    {
                        NotifyEventListenersOnAdded(dependee: dependee, dependency: newInstance);
                    }

                    // Notify instance about completed instantiation
                    (newInstance as IContainerLifecycleListener)?.OnInstantiated();

                    // Notify listeners about added dependency
                    _onInstantiated?.Invoke(newInstance);

                    bool doDestroyInstanceAndScope;

                    lock (this)
                    {
                        // If instance is no longer in loadingInstance then
                        // the container was cleared since it began loading
                        doDestroyInstanceAndScope = !loadingInstances.Remove(newInstance);

                        if (singletonInstance == newInstance)
                        {
                            if (doDestroyInstanceAndScope)
                            {
                                singletonInstance = null;
                            }
                            else
                            {
                                ++reentrancyCount;
                                try
                                {
                                    // Notify observers about added singleton
                                    singletonSubject.OnNext(newInstance);
                                }
                                finally
                                {
                                    --reentrancyCount;
                                }
                            }
                        }
                    }

                    if (doDestroyInstanceAndScope)
                    {
                        destroyInstanceAndScope(newInstance);
                    }
                }
                else
                {
                    destroyInstanceAndScope(newInstance);
                }

                return newInstance;
            }
            catch (Exception)
            {
                destroyInstanceAndScope(newInstance);
                throw;
            }
            finally
            {
                // Ensure that temporary values during loading
                // are cleaned up when an exception occurs

                loadingScope2Dependencies.TryRemove(newScope, out var _);

                lock (this)
                {
                    loadingScopes.Remove(newScope);

                    if (newInstance != null)
                    {
                        loadingInstances.Remove(newInstance);
                    }
                }
            }
        }

        public bool Destroy(T instance)
        {
            if (!isContainerInitialized)
            {
                // There can't be any instances before container
                // is initialized
                return false;
            }

            if (!instance2Scope.ContainsKey(instance) || (isContainerSingleton && singletonInstance != instance))
            {
                // Nothing to do if container doesn't contain the instance.
                // Once removed, it'll never be in the container again
                return false;
            }

            CheckReentrancy();

            IResolverContext? scope = null;
            IEnumerable<IContainerLifecycle>? dependencies = null;

            lock (this)
            {
                if (disposed)
                {
                    // Nothing to do
                    return false;
                }

                if (instance2Scope.TryGetValue(instance, out scope))
                {
                    ++reentrancyCount;
                    try
                    {
                        this.RaisePropertyChanging(nameof(Count));

                        dependencies = scope2Dependencies.TryRemove(scope, out var v) ? v : Enumerable.Empty<IContainerLifecycle>();

                        scopes.Remove(scope);
                        scope2Instance.Remove(scope);
                        instance2Scope.TryRemove(instance, out var _);

                        this.RaisePropertyChanged(nameof(Count));

                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, instance));
                    }
                    finally
                    {
                        --reentrancyCount;
                    }
                }
            }

            if (dependencies != null && scope != null)
            {
                bool doDestroyLifecycle;
                lock (this)
                {
                    // If the instance is in loadingInstances then
                    // the destruction of the instance will be handled
                    // by Instantiate
                    doDestroyLifecycle = !loadingInstances.Remove(instance);
                }

                if (doDestroyLifecycle)
                {
                    lock (this)
                    {
                        if (singletonInstance == instance)
                        {
                            singletonInstance = null;

                            ++reentrancyCount;
                            try
                            {
                                // Notify observers about removed singleton
                                singletonSubject.OnNext(null);
                            }
                            finally
                            {
                                --reentrancyCount;
                            }
                        }
                    }

                    // Notify parent's instance about removed dependency
                    (dependee as IContainerDependencyListener)?.OnRemoved(instance);

                    // Notify instance of being destroyed
                    (instance as IContainerLifecycleListener)?.OnDestroying();

                    // Destroy removed dependencies
                    foreach (var dependency in dependencies)
                    {
                        dependency.Destroy(true);
                    }

                    // Notify instance of having been destroyed
                    (instance as IContainerLifecycleListener)?.OnDestroyed();

                    // Notify listeners about destroyed dependency
                    _onDestroyed?.Invoke(instance);

                    // Dispose scope which will also dispose
                    // the created instances
                    scope.Dispose();
                }

                return true;
            }

            return false;
        }

        public void Destroy()
        {
            DestroyInternal(false);
        }

        void IContainerLifecycle.Destroy(bool dispose)
        {
            DestroyInternal(dispose);
        }

        object? DestroyInternal(bool dispose)
        {
            if (!isContainerInitialized)
            {
                if (dispose)
                {
                    throw new InvalidOperationException($"Cannot dispose before container ({typeof(T).FullName}, {typeof(Template).FullName}) is initialized");
                }

                // There can't be any instances before container
                // is initialized
                return null;
            }

            if (Count == 0 || (isContainerSingleton && singletonInstance == null))
            {
                // Nothing to do if container is already empty
                return null;
            }

            CheckReentrancy();

            Dictionary<T, (IEnumerable<IContainerLifecycle> dependencies, IResolverContext scope)> oldInstances;
            HashSet<IResolverContext> oldScopes;

            object newGenerationKey;

            lock (this)
            {
                if (disposed)
                {
                    // Nothing to do
                    return null;
                }

                // Prevent initialization of new dependencies
                // if the container is being disposed
                if (dispose)
                {
                    disposed = true;
                }

                // Set new generation key so that instantiations
                // started before the container is cleared here
                // and haven't finished yet won't be added to
                // the container and instead be destroyed
                newGenerationKey = generationKey = new object();

                oldInstances = new Dictionary<T, (IEnumerable<IContainerLifecycle> dependencies, IResolverContext scope)>();
                foreach (var entry in scope2Instance)
                {
                    oldInstances.Add(entry.Value, (scope2Dependencies.TryGetValue(entry.Key, out var dependencies) ? dependencies : Enumerable.Empty<IContainerLifecycle>(), entry.Key));
                }

                oldScopes = new HashSet<IResolverContext>(scopes);

                ++reentrancyCount;
                try
                {
                    this.RaisePropertyChanging(nameof(Count));

                    scope2Dependencies.Clear();
                    scopes.Clear();
                    scope2Instance.Clear();
                    instance2Scope.Clear();

                    this.RaisePropertyChanged(nameof(Count));

                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                finally
                {
                    --reentrancyCount;
                }
            }

            foreach (var entry in oldInstances)
            {
                var instance = entry.Key;
                var dependencies = entry.Value.dependencies;
                var scope = entry.Value.scope;

                bool doDestroyLifecycle;
                lock (this)
                {
                    // If the instance is in loadingInstances then
                    // the destruction of the instance will be handled
                    // by Instantiate
                    doDestroyLifecycle = !loadingInstances.Remove(instance);
                }

                if (doDestroyLifecycle)
                {
                    lock (this)
                    {
                        if (singletonInstance == instance)
                        {
                            singletonInstance = null;

                            ++reentrancyCount;
                            try
                            {
                                // Notify observers about removed singleton
                                singletonSubject.OnNext(null);
                            }
                            finally
                            {
                                --reentrancyCount;
                            }
                        }
                    }

                    // Notify parent's instance about removed dependency
                    (dependee as IContainerDependencyListener)?.OnRemoved(instance);

                    // Notify instance of being destroyed
                    (instance as IContainerLifecycleListener)?.OnDestroying();

                    // Destroy removed dependencies
                    foreach (var dependency in dependencies)
                    {
                        dependency.Destroy(true);
                    }

                    // Notify instance of having been destroyed
                    (instance as IContainerLifecycleListener)?.OnDestroyed();

                    // Notify listeners about destroyed dependency
                    _onDestroyed?.Invoke(instance);

                    // Dispose scope which will also dispose
                    // the created instances
                    scope.Dispose();
                    oldScopes.Remove(scope);
                }
            }

            // Dispose scopes which will also dispose
            // the created instances
            foreach (var scope in oldScopes)
            {
                scope.Dispose();
            }

            if (dispose)
            {
                lock (this)
                {
                    ++reentrancyCount;
                    try
                    {
                        singletonSubject.OnCompleted();
                    }
                    finally
                    {
                        --reentrancyCount;
                    }

                    singletonSubject.Dispose();
                }

                // Child container is not disposed because
                // disposal of services is already handled
                // by the scope, and disposing the child
                // container would cause unintentional
                // disposal of other services due to the
                // container's scopes being shared instead
                // of cloned
            }

            return newGenerationKey;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var entry in instance2Scope)
            {
                yield return entry.Key;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var entry in instance2Scope)
            {
                yield return entry.Key;
            }
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
            public static IContainer<TT, TTemplate> CreateAndTrackDependency<TT, TTemplate>(IContainer<TT, TTemplate> container, IResolverContext scope, Action<IContainerLifecycle, IResolverContext> action)
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
