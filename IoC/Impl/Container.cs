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
        private IContainerLifecycle? parent;
        private object? dependee;

        private bool isContainerSingleton;

        private bool _isSingleton;
        public bool IsSingleton
        {
            get => _isSingleton;
            private set
            {
                if (_isSingleton != value)
                {
                    PropertyChanging?.Invoke(this, new(nameof(IsSingleton)));
                    _isSingleton = value;
                    PropertyChanged?.Invoke(this, new(nameof(IsSingleton)));
                }
            }
        }

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
        private readonly object? scopeName;

        public Container(IContainerResolver resolver, Func<IRegistrar<Instance, Parameter>, Instance> regCtor)
        {
            // Create child container so that the T registration
            // can be replaced/shadowed
            this.resolver = resolver = resolver.CreateChild();

            // Register initializer to track injected dependencies
            resolver.RegisterInitializer(OnDependencyInjected, key =>
            {
                lock (this)
                {
                    return scopeKeys.Contains(key) || loadingScopeKeys.Contains(key);
                }
            });

            // Invoke factory constructor to complete registration
            RegistrationCompletion? completion = null;
            try
            {
                regCtor.Invoke(new Registrar(resolver));
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

        public IContainer<Instance, Parameter> ToSingleton()
        {
            ChangeToSingleton();
            return this;
        }

        public IObservable<Instance?> ToSingletonWithObservable()
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

        private void OnDependencyInjected(IContainerLifecycle lifecycle, IContainerResolver.IScope scope)
        {
            CheckReentrancy();

            Instance? injectedInstance = null;

            lock (this)
            {
                CheckDisposed();

                if (!scopeKeys.Contains(scope.Key) && !loadingScopeKeys.Contains(scope.Key))
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
            // passed by T to the registrar
            newScope = resolver.OpenScope(scopeName);

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
                    throw new InvalidOperationException($"Cannot dispose before container ({typeof(Instance).FullName}, {typeof(Parameter).FullName}) is initialized");
                }

                // There can't be any instances before container
                // is initialized
                return null;
            }

            if (Count == 0 || isContainerSingleton && singletonInstance == null)
            {
                // Nothing to do if container is already empty
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
            private readonly IContainerResolver resolver;

            public Registrar(IContainerResolver resolver)
            {
                this.resolver = resolver;
            }

            public object ClassScopeName => resolver.GetClassScopeName<Instance>();

            [DoesNotReturn]
            public void RegisterAndReturn<TImpl>(object? scopeName = null, ConstructorInfo? constructor = null)
                where TImpl : class, Instance
            {
                // Register implementation with appropriate reuse and factory

                if (constructor != null)
                {
                    if (constructor.DeclaringType != typeof(TImpl))
                    {
                        throw new InvalidOperationException($"Constructor is not declared by {typeof(TImpl).FullName}");
                    }
                }
                else
                {
                    foreach (var ctor in typeof(TImpl).GetTypeInfo().DeclaredConstructors)
                    {
                        if (ctor.GetCustomAttribute<InstantiatorAttribute>() != null)
                        {
                            if (constructor != null)
                            {
                                throw new InvalidOperationException($"Multiple instantiator constructors in {typeof(TImpl).FullName}");
                            }
                            constructor = ctor;
                        }
                    }
                }

                resolver.RegisterService<Instance, TImpl>(constructor);

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
    }
}
