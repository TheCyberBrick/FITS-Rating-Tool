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
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class FactoryBuilder<T, Template> : IFactoryBuilder<T, Template>
        where T : class
    {
        private abstract class GenericFactory : IGenericFactory<T, Template>, IDisposable
        {
            public bool IsSingleUse { get; private set; }

            private bool expired;

            private readonly Func<Template?> templateConstructor;

            public GenericFactory(Func<Template?> templateConstructor, bool isSingleUse)
            {
                this.templateConstructor = templateConstructor;
                IsSingleUse = isSingleUse;
            }

            IFactoryBase<T> IFactoryBase<T>.AndThen(Action<T> action) => AndThen(action);

            public abstract IGenericFactory<T, Template> AndThen(Action<T> action);

            public abstract T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable);

            protected T InstantiateInternal(Func<Template, T?> instanceConstructor)
            {
                CheckExpired();

                if (IsSingleUse)
                {
                    Expire();
                }

                var template = templateConstructor.Invoke();

                if (template == null)
                {
                    Dispose();
                    CheckExpired();
                }

                var instance = instanceConstructor.Invoke(template!);

                if (instance == null)
                {
                    Dispose();
                    CheckExpired();
                }

                return instance!;
            }

            private void CheckExpired()
            {
                if (expired)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }

            protected void Expire()
            {
                expired = true;
            }

            public abstract void Dispose();
        }

        private class TemplatedFactory : GenericFactory, ITemplatedFactory<T, Template>
        {
            public TemplatedFactory(Func<Template?> templateConstructor, bool isSingleUse) : base(templateConstructor, isSingleUse)
            {
            }

            ITemplatedFactory<T, Template> ITemplatedFactory<T, Template>.AndThen(Action<T> action) => new ChildTemplatedFactory(this, action);

            public override IGenericFactory<T, Template> AndThen(Action<T> action) => new ChildTemplatedFactory(this, action);

            public T Instantiate(Func<Template, T> instanceConstructor)
            {
                lock (this)
                {
                    return InstantiateInternal(instanceConstructor);
                }
            }

            public override T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable)
            {
                var newInstance = Instantiate(instanceConstructor);
                disposable = Disposable.Create(() => instanceDestructor.Invoke(newInstance));
                return newInstance;
            }

            public override void Dispose()
            {
                lock (this)
                {
                    Expire();
                }
            }
        }

        private class ChildTemplatedFactory : ITemplatedFactory<T, Template>
        {
            public bool IsSingleUse => parent.IsSingleUse;

            private readonly ITemplatedFactory<T, Template> parent;
            private readonly Action<T> action;

            public ChildTemplatedFactory(ITemplatedFactory<T, Template> parent, Action<T> action)
            {
                this.parent = parent;
                this.action = action;
            }

            public ITemplatedFactory<T, Template> AndThen(Action<T> action) => new ChildTemplatedFactory(this, action);

            IGenericFactory<T, Template> IGenericFactory<T, Template>.AndThen(Action<T> action) => new ChildTemplatedFactory(this, action);

            IFactoryBase<T> IFactoryBase<T>.AndThen(Action<T> action) => new ChildTemplatedFactory(this, action);

            public T Instantiate(Func<Template, T> instanceConstructor)
            {
                var newInstance = parent.Instantiate(instanceConstructor);
                action.Invoke(newInstance);
                return newInstance;
            }

            public T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable)
            {
                var newInstance = parent.Instantiate(instanceConstructor, instanceDestructor, out disposable);
                action.Invoke(newInstance);
                return newInstance;
            }
        }

        private class DelegatedFactoryDisposer : IDisposable
        {
            private readonly List<T> instances = new();

            private readonly FactoryBuilder<T, Template> factory;
            private readonly Action<T> instanceDestructor;

            public DelegatedFactoryDisposer(FactoryBuilder<T, Template> factory, Action<T> instanceDestructor)
            {
                this.instanceDestructor = instanceDestructor;
                this.factory = factory;
            }

            public void AddInstance(T instance)
            {
                lock (this)
                {
                    instances.Add(instance);

                    factory.AddDisposer(this);
                }
            }

            public void RemoveInstance(T instance)
            {
                lock (this)
                {
                    if (instances.Remove(instance))
                    {
                        instanceDestructor.Invoke(instance);
                    }

                    if (instances.Count == 0)
                    {
                        factory.RemoveDisposer(this);
                    }
                }
            }

            public void Dispose()
            {
                lock (this)
                {
                    for (int i = instances.Count - 1; i >= 0; --i)
                    {
                        var instance = instances[i];
                        instances.RemoveAt(i);
                        instanceDestructor.Invoke(instance);
                    }

                    if (instances.Count == 0)
                    {
                        factory.RemoveDisposer(this);
                    }
                }
            }
        }

        private class DelegatedFactory : GenericFactory, IDelegatedFactory<T, Template>
        {
            private readonly Func<Template, T?> instanceConstructor;
            private readonly DelegatedFactoryDisposer disposer;

            public DelegatedFactory(Func<Template?> templateConstructor, Func<Template, T?> instanceConstructor, bool isSingleUse, DelegatedFactoryDisposer disposer) : base(templateConstructor, isSingleUse)
            {
                this.instanceConstructor = instanceConstructor;
                this.disposer = disposer;
            }

            IDelegatedFactory<T, Template> IDelegatedFactory<T, Template>.AndThen(Action<T> action) => new ChildDelegatedFactory(this, action);

            IDelegatedFactory<T> IDelegatedFactory<T>.AndThen(Action<T> action) => new ChildDelegatedFactory(this, action);

            public override IGenericFactory<T, Template> AndThen(Action<T> action) => new ChildDelegatedFactory(this, action);

            public T Instantiate(out IDisposable disposable)
            {
                lock (disposer)
                {
                    var newInstance = InstantiateInternal(instanceConstructor);

                    disposer.AddInstance(newInstance);

                    disposable = Disposable.Create(() => disposer.RemoveInstance(newInstance));

                    return newInstance;
                }
            }

            public override T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable)
            {
                return Instantiate(out disposable);
            }

            public override void Dispose()
            {
                lock (disposer)
                {
                    Expire();

                    disposer.Dispose();
                }
            }
        }

        private class ChildDelegatedFactory : IDelegatedFactory<T, Template>
        {
            public bool IsSingleUse => parent.IsSingleUse;

            private readonly IDelegatedFactory<T, Template> parent;
            private readonly Action<T> action;

            public ChildDelegatedFactory(IDelegatedFactory<T, Template> parent, Action<T> action)
            {
                this.parent = parent;
                this.action = action;
            }

            public IDelegatedFactory<T, Template> AndThen(Action<T> action) => new ChildDelegatedFactory(this, action);

            IDelegatedFactory<T> IDelegatedFactory<T>.AndThen(Action<T> action) => new ChildDelegatedFactory(this, action);

            IGenericFactory<T, Template> IGenericFactory<T, Template>.AndThen(Action<T> action) => new ChildDelegatedFactory(this, action);

            IFactoryBase<T> IFactoryBase<T>.AndThen(Action<T> action) => new ChildDelegatedFactory(this, action);

            public T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable)
            {
                var newInstance = parent.Instantiate(instanceConstructor, instanceDestructor, out disposable);
                action.Invoke(newInstance);
                return newInstance;
            }

            public T Instantiate(out IDisposable disposable)
            {
                var newInstance = parent.Instantiate(out disposable);
                action.Invoke(newInstance);
                return newInstance;
            }
        }

        private readonly List<WeakReference<GenericFactory>> factories = new();
        private readonly List<DelegatedFactoryDisposer> disposers = new();

        private bool disposed;

        public ITemplatedFactory<T, Template> Templated(Func<Template?> templateConstructor, bool isSingleUse)
        {
            var factory = new TemplatedFactory(templateConstructor, isSingleUse);
            AddFactory(factory);
            return factory;
        }

        public IDelegatedFactory<T, Template> Delegated(Func<Template?> templateConstructor, Func<Template, T?> instanceConstructor, Action<T> instanceDestructor, bool isSingleUse)
        {
            var disposer = new DelegatedFactoryDisposer(this, instanceDestructor);
            var factory = new DelegatedFactory(templateConstructor, instanceConstructor, isSingleUse, disposer);
            AddFactory(factory);
            return factory;
        }

        private void AddFactory(GenericFactory factory)
        {
            lock (factories)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                // Remove any factories that are no
                // longer referenced and thus can't
                // be instantiated anymore -> no need
                // to dispose them anymore later
                for (int i = factories.Count - 1; i >= 0; --i)
                {
                    if (!factories[i].TryGetTarget(out var _))
                    {
                        factories.RemoveAt(i);
                    }
                }

                factories.Add(new(factory));
            }
        }

        private void AddDisposer(DelegatedFactoryDisposer disposer)
        {
            lock (disposers)
            {
                if (!disposers.Contains(disposer))
                {
                    disposers.Add(disposer);
                }
            }
        }

        private void RemoveDisposer(DelegatedFactoryDisposer disposer)
        {
            lock (disposers)
            {
                disposers.Remove(disposer);
            }
        }

        public void Dispose()
        {
            lock (disposers)
            {
                lock (factories)
                {
                    disposed = true;

                    // Dispose all factories that are still
                    // referenced somewhere so that they
                    // can't be used anymore
                    for (int i = factories.Count - 1; i >= 0; --i)
                    {
                        if (factories[i].TryGetTarget(out var factory))
                        {
                            factory.Dispose();
                        }
                        factories.RemoveAt(i);
                    }

                    // Dispose any "left over" delegated
                    // factory disposers to make sure that
                    // their instances are deconstructed
                    for (int i = disposers.Count - 1; i >= 0; --i)
                    {
                        disposers[i].Dispose();
                        disposers.RemoveAt(i);
                    }
                }
            }
        }
    }
}
