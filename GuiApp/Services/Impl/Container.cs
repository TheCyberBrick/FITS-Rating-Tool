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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reflection;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class Container<T, Template> : ReactiveObject, IContainer<T, Template>, IContainerLifecycle, IObservable<T?>
        where T : class
    {
        private object? _owner;
        public object? Owner
        {
            get => _owner;
            private set => this.RaiseAndSetIfChanged(ref _owner, value);
        }

        private IContainerLifecycle? parent;


        private T? _instance;

        public T Instance => _instance ?? throw new InvalidOperationException("Instance is null");

        public T? InstanceOrNull => _instance;

        object? IContainerLifecycle.Instance => _instance;


        private bool _initialized;

        public bool IsInitialized
        {
            get => _initialized;
            set => this.RaiseAndSetIfChanged(ref _initialized, value);
        }

        public IObservable<T?> WhenChanged => this;

        private Action<Template?>? _onInitialized;
        public event Action<Template?> OnInitialized
        {
            add => _onInitialized += value;
            remove => _onInitialized -= value;
        }


        private bool disposed;

        private Template? template;

        private readonly List<IContainerLifecycle> dependencies = new();

        private readonly object? scopeName;
        private IResolverContext? scope;

        private readonly List<IObserver<T?>> observers = new();


        private readonly IContainer container;

        public Container(IContainer container, Func<IRegistrar<T, Template>, T> regCtor)
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
                    parameters: Parameters.Of.Type(req => (Action<IContainerLifecycle>)OnDependencyInjected)),
                setup: Setup.DecoratorWith(
                    r => r.Container == scope &&
                    r.FactoryType != FactoryType.Wrapper &&
                    r.ServiceType.IsGenericType &&
                    typeof(IContainer<,>).IsAssignableFrom(r.ServiceType.GetGenericTypeDefinition()),
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

        private void OnDependencyInjected(IContainerLifecycle lifecycle)
        {
            lock (this)
            {
                CheckDisposed();

                dependencies.Add(lifecycle);

                if (_instance != null)
                {
                    // Dependency was created after the
                    // instance has already been constructed
                    // -> initialize injected dependency now
                    lifecycle.Initialize(this, _instance);
                }
            }
        }

        void IContainerLifecycle.Initialize(IContainerLifecycle? parent, object? owner)
        {
            lock (this)
            {
                Owner = owner;
                this.parent = parent;

                IsInitialized = true;

                var initTemplate = template;

                if (initTemplate != null)
                {
                    try
                    {
                        Instantiate(initTemplate);
                    }
                    finally
                    {
                        template = default;
                    }
                }

                _onInitialized?.Invoke(initTemplate);
            }
        }

        private static void NotifyEventListenersOnAdded(object dependee, object dependency)
        {
            // Notify dependency about being added to dependee
            (dependency as IContainerEvents)?.OnAddedTo(dependee);

            // Notify dependee about added dependency
            (dependee as IContainerEvents)?.OnAdded(dependency);
        }

        public IContainer<T, Template> Instantiate(Template template)
        {
            lock (this)
            {
                if (!IsInitialized)
                {
                    this.template = template;
                }
                else
                {
                    Clear();

                    // Open a new scope with the scope previously
                    // passed by T to the registrar
                    scope = container.OpenScope(scopeName);

                    try
                    {
                        // Create a new instance
                        var newInstance = scope.Resolve<Func<Template, T>>().Invoke(template);

                        // Initialize injected dependencies
                        foreach (var dependency in dependencies)
                        {
                            dependency.Initialize(this, newInstance);

                            var dependencyInstance = dependency.Instance;
                            if (dependencyInstance != null)
                            {
                                NotifyEventListenersOnAdded(dependee: newInstance, dependency: dependencyInstance);
                            }
                        }

                        if (_instance != newInstance)
                        {
                            this.RaisePropertyChanging(nameof(Instance));
                            this.RaisePropertyChanging(nameof(InstanceOrNull));

                            _instance = newInstance;

                            this.RaisePropertyChanged(nameof(Instance));
                            this.RaisePropertyChanged(nameof(InstanceOrNull));

                            var parentInstance = parent?.Instance;
                            if (parentInstance != null)
                            {
                                NotifyEventListenersOnAdded(dependee: parentInstance, dependency: newInstance);
                            }

                            // Notify observers about added dependency
                            foreach (var observer in observers)
                            {
                                observer.OnNext(newInstance);
                            }
                        }

                        // Notify instance about completed instantiation
                        (newInstance as IContainerEvents)?.OnInstantiated();
                    }
                    catch (Exception ex)
                    {
                        Clear();

                        foreach (var observer in observers)
                        {
                            observer.OnError(ex);
                        }

                        throw;
                    }
                }
            }

            return this;
        }

        public void Clear()
        {
            ((IContainerLifecycle)this).Clear(false);
        }

        void IContainerLifecycle.Clear(bool dispose)
        {
            lock (this)
            {
                template = default;

                // Prevent initialization of new dependencies
                // if the container is being disposed
                if (dispose)
                {
                    disposed = true;
                }

                // Clear and dispose all dependencies first
                foreach (var dependency in dependencies)
                {
                    dependency.Clear(true);
                }
                dependencies.Clear();

                if (_instance != null)
                {
                    var oldInstance = _instance;

                    this.RaisePropertyChanging(nameof(Instance));
                    this.RaisePropertyChanging(nameof(InstanceOrNull));

                    _instance = null;

                    this.RaisePropertyChanged(nameof(Instance));
                    this.RaisePropertyChanged(nameof(InstanceOrNull));

                    // Notify parent's instance about removed dependency
                    (parent?.Instance as IContainerEvents)?.OnRemoved(oldInstance);

                    // Notify observers about removed dependency
                    foreach (var observer in observers)
                    {
                        observer.OnNext(null);
                    }
                }

                // Dispose scope which will also dispose
                // the created instances
                scope?.Dispose();
                scope = null;

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

        public void Dispose()
        {
            ((IContainerLifecycle)this).Clear(true);
        }

        public IDisposable Subscribe(IObserver<T?> observer)
        {
            lock (this)
            {
                CheckDisposed();
                observers.Add(observer);
                observer.OnNext(InstanceOrNull);
            }
            return Disposable.Create(() =>
            {
                lock (this)
                {
                    observers.Remove(observer);
                }
            });
        }

        private class Registrar : IRegistrar<T, Template>
        {
            private readonly IContainer container;

            public Registrar(IContainer container)
            {
                this.container = container;
            }

            public object ClassScope => ResolutionScopeName.Of<T>();

            [DoesNotReturn]
            public void RegisterAndReturn<TImpl>(object? scope = null, ConstructorInfo? constructor = null)
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

                throw new RegistrationCompletion(typeof(TImpl), scope);
            }
        }

        private class RegistrationCompletion : Exception
        {
            public Type RegisteredType { get; }

            public object? ScopeName { get; }

            public RegistrationCompletion(Type registeredType, object? scope)
            {
                RegisteredType = registeredType;
                ScopeName = scope;
            }
        }

        private static class DependencyTracker
        {
            public static IContainer<TT, TTemplate> OnDependencyInjected<TT, TTemplate>(IContainer<TT, TTemplate> container, Action<IContainerLifecycle> action)
                where TT : class
            {
                if (container is IContainerLifecycle lifecycle)
                {
                    action.Invoke(lifecycle);
                }
                return container;
            }
        }
    }
}
