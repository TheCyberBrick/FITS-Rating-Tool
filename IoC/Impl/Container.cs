﻿/*
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

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Reflection;

namespace FitsRatingTool.IoC.Impl
{
    [Export(typeof(IContainer<,>)), PartCreationPolicy(CreationPolicy.NonShared)]
    public class Container<Instance, Parameter> : IContainer<Instance, Parameter>, IContainerLifecycle, INotifyPropertyChanged, INotifyPropertyChanging
        where Instance : class
    {
        private IContainerResolver.IScope? scope;
        private IContainerLifecycle? parent;
        private object? dependee;

        public Type InstanceType => typeof(Instance);

        public Type ParameterType => typeof(Parameter);

        private bool isContainerSingleton;

        private ISingletonContainer<Instance, Parameter>? singletonWrapper;
        public bool IsSingleton => singletonWrapper != null;

        public int Count => instance2Scope.Count;


        private bool isContainerInitialized;

        private bool _initialized;
        public bool IsInitialized
        {
            get => _initialized;
            private set
            {
                if (_initialized != value)
                {
                    PropertyChanging?.Invoke(this, new(nameof(IsInitialized)));
                    _initialized = value;
                    PropertyChanged?.Invoke(this, new(nameof(IsInitialized)));
                }
            }
        }

        private Action? _onInitialized;
        public event Action OnInitialized
        {
            add => _onInitialized += value;
            remove => _onInitialized -= value;
        }

        private Action<Instance>? _onInstantiated;
        public event Action<Instance> OnInstantiated
        {
            add => _onInstantiated += value;
            remove => _onInstantiated -= value;
        }

        private Action<Instance>? _onDestroyed;
        public event Action<Instance> OnDestroyed
        {
            add => _onDestroyed += value;
            remove => _onDestroyed -= value;
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Dictionary<IContainerResolver.IScope, Instance> scope2Instance = new();
        private readonly ConcurrentDictionary<Instance, IContainerResolver.IScope> instance2Scope = new();

        private readonly ConcurrentDictionary<IContainerResolver.IScope, List<IContainerLifecycle>> scope2Dependencies = new();
        private readonly HashSet<IContainerResolver.IScope> scopes = new();
        private readonly HashSet<object> scopeKeys = new();


        private readonly ConcurrentDictionary<IContainerResolver.IScope, List<IContainerLifecycle>> loadingScope2Dependencies = new();
        private readonly HashSet<object> loadingScopeKeys = new();

        private readonly HashSet<Instance> loadingInstances = new();

        private readonly ReplaySubject<Instance?> singletonSubject = new(bufferSize: 1);

        private object generationKey = new();

        private Instance? singletonInstance;

        private bool disposed;

        private int reentrancyCount;

        private readonly IContainerResolver resolver;
        private readonly object[] scopeNames;
        private readonly Func<Parameter, Parameter>? initializer;

        public Container(IContainerResolver parentResolver, Func<IRegistrar<Instance, Parameter>, Instance> regCtor)
        {
            // Invoke factory constructor to complete registration
            RegistrationCompletion? completion = null;
            try
            {
                regCtor.Invoke(new Registrar(this, parentResolver));
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

            resolver = completion.Fork;
            scopeNames = completion.ScopeNames;
            initializer = completion.Initializer;
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

        public ISingletonContainer<Instance, Parameter> Singleton()
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

            if (singletonWrapper == null)
            {
                PropertyChanging?.Invoke(this, new(nameof(IsSingleton)));
                singletonWrapper = new SingletonContainerWrapper<Instance, Parameter>(this, singletonSubject);
                PropertyChanged?.Invoke(this, new(nameof(IsSingleton)));
            }

            return singletonWrapper;
        }

        private bool ContainsScope(object scopeKey)
        {
            lock (this)
            {
                return scopeKeys.Contains(scopeKey) || loadingScopeKeys.Contains(scopeKey);
            }
        }

        private void OnDependencyInjected(IContainerLifecycle lifecycle, IContainerResolver.IScope scope)
        {
            CheckReentrancy();

            Instance? injectedInstance = null;

            lock (this)
            {
                CheckDisposed();

                if (!scopeKeys.Contains(scope.Key) && !loadingScopeKeys.Contains(scope.Key))
                {
                    if (scope.IsRootScope)
                    {
                        // If scope is a root scope then we ignore
                        // the injected instance because it will
                        // form another dependency tree.
                        // However, previously already created scopes
                        // and scoped instances can be propagated into
                        // the new instance's dependency tree.
                        return;
                    }
                    else
                    {
                        throw new InvalidOperationException("Dependency was injected with unknown scope");
                    }
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
                lifecycle.Initialize(scope, this, injectedInstance);
            }
        }

        void IContainerLifecycle.Initialize(IContainerResolver.IScope? scope, IContainerLifecycle? parent, object? dependee)
        {
            CheckReentrancy();

            if (IsInitialized)
            {
                throw new InvalidOperationException("Container is already initialized");
            }

            lock (this)
            {
                CheckDisposed();

                this.parent = parent;
                this.scope = scope;
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

        public Instance Instantiate(Parameter parameter)
        {
            CheckReentrancy();

            IContainerResolver.IScope newScope;
            object? newGenerationKey;
            bool clearContainer = isContainerSingleton;

            lock (this)
            {
                CheckDisposed();

                if (!isContainerInitialized)
                {
                    throw new InvalidOperationException($"Cannot instantiate before container ({typeof(Instance).FullName}, {typeof(Parameter).FullName}) is initialized");
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
            // passed to the registrar
            newScope = resolver.OpenScopes(scope, false, scopeNames);
            if (newScope.IsRootScope)
            {
                newScope.Dispose();
                throw new InvalidOperationException("Opened scope should not be a root scope");
            }

            void destroyInstanceAndScope(Instance? instance)
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
                        scopeKeys.Remove(newScope.Key);
                    }
                    if (disposeScope)
                    {
                        newScope.Dispose();
                    }
                }
            }

            Instance? newInstance = null;
            try
            {
                lock (this)
                {
                    // Add new scope to loadingScopes so that
                    // when the container is cleared it won't
                    // destroy the new instance or dispose the
                    // new scope because Instantiate will take
                    // care of the destruction
                    loadingScopeKeys.Add(newScope.Key);
                }

                // Run initializer if set
                if (initializer != null)
                {
                    parameter = initializer.Invoke(parameter);
                }

                // Create a new instance.
                // Passes newScope explicitly to make sure that
                // OnDependencyInjected receives the same scope
                // instance so that it can track the dependencies
                // properly
                newInstance = newScope.Resolve<Instance, Parameter>(parameter);

                List<IContainerLifecycle>? dependencies = null;

                bool added = false;

                lock (this)
                {
                    CheckDisposed();

                    loadingScopeKeys.Remove(newScope.Key);
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
                            PropertyChanging?.Invoke(this, new(nameof(Count)));

                            if (loadingScope2Dependencies.TryRemove(newScope, out dependencies))
                            {
                                scope2Dependencies.TryAdd(newScope, dependencies);
                            }

                            scopes.Add(newScope);
                            scopeKeys.Add(newScope.Key);
                            scope2Instance.Add(newScope, newInstance);
                            instance2Scope.TryAdd(newInstance, newScope);

                            PropertyChanged?.Invoke(this, new(nameof(Count)));

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
                            dependency.Initialize(newScope, this, newInstance);
                        }
                    }

                    if (dependee != null)
                    {
                        NotifyEventListenersOnAdded(dependee: dependee, dependency: newInstance);
                    }

                    // Notify instance about completed instantiation
                    (newInstance as ILifecycleSubscriber)?.OnInstantiated();

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
                    loadingScopeKeys.Remove(newScope.Key);

                    if (newInstance != null)
                    {
                        loadingInstances.Remove(newInstance);
                    }
                }
            }
        }

        public bool Destroy(Instance instance)
        {
            if (!isContainerInitialized)
            {
                // There can't be any instances before container
                // is initialized
                return false;
            }

            if (!instance2Scope.ContainsKey(instance) || isContainerSingleton && singletonInstance != instance)
            {
                // Nothing to do if container doesn't contain the instance.
                // Once removed, it'll never be in the container again
                return false;
            }

            CheckReentrancy();

            IContainerResolver.IScope? scope = null;
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
                        PropertyChanging?.Invoke(this, new(nameof(Count)));

                        dependencies = scope2Dependencies.TryRemove(scope, out var v) ? v : Enumerable.Empty<IContainerLifecycle>();

                        scopes.Remove(scope);
                        scopeKeys.Remove(scope.Key);
                        scope2Instance.Remove(scope);
                        instance2Scope.TryRemove(instance, out var _);

                        PropertyChanged?.Invoke(this, new(nameof(Count)));

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
                    (instance as ILifecycleSubscriber)?.OnDestroying();

                    // Destroy removed dependencies
                    foreach (var dependency in dependencies)
                    {
                        dependency.Destroy(true);
                    }

                    // Notify instance of having been destroyed
                    (instance as ILifecycleSubscriber)?.OnDestroyed();

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
                    throw new InvalidOperationException($"Cannot dispose before container ({typeof(Instance).FullName}, {typeof(Parameter).FullName}) is initialized");
                }

                // There can't be any instances before container
                // is initialized
                return null;
            }

            if (!dispose && (Count == 0 || isContainerSingleton && singletonInstance == null))
            {
                // Nothing to do if container is already empty
                // and doesn't need to be disposed
                return null;
            }

            CheckReentrancy();

            Dictionary<Instance, (IEnumerable<IContainerLifecycle> dependencies, IContainerResolver.IScope scope)> oldInstances;
            HashSet<IContainerResolver.IScope> oldScopes;

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

                oldInstances = new Dictionary<Instance, (IEnumerable<IContainerLifecycle> dependencies, IContainerResolver.IScope scope)>();
                foreach (var entry in scope2Instance)
                {
                    oldInstances.Add(entry.Value, (scope2Dependencies.TryGetValue(entry.Key, out var dependencies) ? dependencies : Enumerable.Empty<IContainerLifecycle>(), entry.Key));
                }

                oldScopes = new HashSet<IContainerResolver.IScope>(scopes);

                ++reentrancyCount;
                try
                {
                    PropertyChanging?.Invoke(this, new(nameof(Count)));

                    scope2Dependencies.Clear();
                    scopes.Clear();
                    scopeKeys.Clear();
                    scope2Instance.Clear();
                    instance2Scope.Clear();

                    PropertyChanged?.Invoke(this, new(nameof(Count)));

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
                    (instance as ILifecycleSubscriber)?.OnDestroying();

                    // Destroy removed dependencies
                    foreach (var dependency in dependencies)
                    {
                        dependency.Destroy(true);
                    }

                    // Notify instance of having been destroyed
                    (instance as ILifecycleSubscriber)?.OnDestroyed();

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

                    // Allow resolver implementation to do
                    // cleanup if necessary
                    resolver.DestroyFork();
                }
            }

            return newGenerationKey;
        }

        public IEnumerator<Instance> GetEnumerator()
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

        private class Registrar : IRegistrar<Instance, Parameter>
        {
            private readonly Container<Instance, Parameter> container;
            private readonly IContainerResolver parentResolver;

            private ConstructorInfo? constructor;
            private object[] scopeNames = Array.Empty<object>();
            private Func<Parameter, Parameter>? initializer;

            public Registrar(Container<Instance, Parameter> container, IContainerResolver parentResolver)
            {
                this.container = container;
                this.parentResolver = parentResolver;
            }

            public object ClassScopeName => parentResolver.GetClassScopeName<Instance>();

            public IRegistrar<Instance, Parameter> WithConstructor(ConstructorInfo constructor)
            {
                this.constructor = constructor;
                return this;
            }

            public IRegistrar<Instance, Parameter> WithScopes(params object[] scopeNames)
            {
                this.scopeNames = scopeNames;
                return this;
            }

            public IRegistrar<Instance, Parameter> WithInitializer(Func<Parameter, Parameter> initializer)
            {
                this.initializer = initializer;
                return this;
            }

            [DoesNotReturn]
            public void RegisterAndReturn<Implementation>()
                where Implementation : class, Instance
            {
                // Register implementation with appropriate reuse and factory

                if (constructor != null)
                {
                    if (constructor.DeclaringType != typeof(Implementation))
                    {
                        throw new InvalidOperationException($"Constructor is not declared by {typeof(Implementation).FullName}");
                    }
                }
                else
                {
                    foreach (var ctor in typeof(Implementation).GetTypeInfo().DeclaredConstructors)
                    {
                        if (ctor.GetCustomAttribute<InstantiatorAttribute>() != null)
                        {
                            if (constructor != null)
                            {
                                throw new InvalidOperationException($"Multiple instantiator constructors in {typeof(Implementation).FullName}");
                            }
                            constructor = ctor;
                        }
                    }
                }

                // Fork the resolver and register the constructor
                // and initializer to it
                var fork = parentResolver.Fork<Instance, Implementation>(constructor, container.OnDependencyInjected, container.ContainsScope);

                throw new RegistrationCompletion(typeof(Implementation), scopeNames, initializer, fork);
            }
        }

        private class RegistrationCompletion : Exception
        {
            public Type RegisteredType { get; }

            public object[] ScopeNames { get; }

            public Func<Parameter, Parameter>? Initializer { get; }

            public IContainerResolver Fork { get; }

            public RegistrationCompletion(Type registeredType, object[] scopeNames, Func<Parameter, Parameter>? initializer, IContainerResolver fork)
            {
                RegisteredType = registeredType;
                ScopeNames = scopeNames;
                Initializer = initializer;
                Fork = fork;
            }
        }
    }
}
