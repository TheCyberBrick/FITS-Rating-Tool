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
    public class InstantiatorFactory<T, Template> : IInstantiatorFactory<T, Template>
        where T : class
    {
        private abstract class GenericInstantiator : IGenericInstantiator<T, Template>, IDisposable
        {
            public bool IsExpired { get; private set; }

            private readonly Func<Template?> templateConstructor;

            private readonly bool allowMultipleInstantiations;

            public GenericInstantiator(Func<Template?> templateConstructor, bool allowMultipleInstantiations)
            {
                this.templateConstructor = templateConstructor;
                this.allowMultipleInstantiations = allowMultipleInstantiations;
            }

            public abstract IGenericInstantiator<T, Template> AndThen(Action<T> action);

            IInstantiatorBase<T> IInstantiatorBase<T>.AndThen(Action<T> action) => AndThen(action);

            public abstract T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable);

            protected T InstantiateInternal(Func<Template, T?> instanceConstructor)
            {
                CheckExpired();

                if (!allowMultipleInstantiations)
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
                if (IsExpired)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }

            protected void Expire()
            {
                IsExpired = true;
            }

            public abstract void Dispose();
        }

        private class TemplatedInstantiator : GenericInstantiator, ITemplatedInstantiator<T, Template>
        {
            public TemplatedInstantiator(Func<Template?> templateConstructor, bool allowMultipleInstantiations) : base(templateConstructor, allowMultipleInstantiations)
            {
            }

            ITemplatedInstantiator<T, Template> ITemplatedInstantiator<T, Template>.AndThen(Action<T> action) => new ChildTemplatedInstantiator(this, action);

            public override IGenericInstantiator<T, Template> AndThen(Action<T> action) => new ChildTemplatedInstantiator(this, action);

            public T Instantiate(Func<Template, T> instanceConstructor)
            {
                lock (this)
                {
                    return InstantiateInternal(instanceConstructor);
                }
            }

            public override T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable)
            {
                lock (this)
                {
                    var newInstance = Instantiate(instanceConstructor);
                    disposable = Disposable.Create(() => instanceDestructor.Invoke(newInstance));
                    return newInstance;
                }
            }

            public override void Dispose()
            {
                lock (this)
                {
                    Expire();
                }
            }
        }

        private class ChildTemplatedInstantiator : ITemplatedInstantiator<T, Template>
        {
            private readonly ITemplatedInstantiator<T, Template> parent;
            private readonly Action<T> action;

            public ChildTemplatedInstantiator(ITemplatedInstantiator<T, Template> parent, Action<T> action)
            {
                this.parent = parent;
                this.action = action;
            }

            public bool IsExpired => parent.IsExpired;

            public ITemplatedInstantiator<T, Template> AndThen(Action<T> action) => new ChildTemplatedInstantiator(this, action);

            IGenericInstantiator<T, Template> IGenericInstantiator<T, Template>.AndThen(Action<T> action) => new ChildTemplatedInstantiator(this, action);

            IInstantiatorBase<T> IInstantiatorBase<T>.AndThen(Action<T> action) => new ChildTemplatedInstantiator(this, action);

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

        private class DelegatedInstantiatorDisposer : IDisposable
        {
            private readonly List<T> instances = new();

            private readonly InstantiatorFactory<T, Template> factory;
            private readonly Action<T> instanceDestructor;

            public DelegatedInstantiatorDisposer(InstantiatorFactory<T, Template> factory, Action<T> instanceDestructor)
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

        private class DelegatedInstantiator : GenericInstantiator, IDelegatedInstantiator<T, Template>
        {
            private readonly Func<Template, T?> instanceConstructor;
            private readonly DelegatedInstantiatorDisposer disposer;

            public DelegatedInstantiator(Func<Template?> templateConstructor, Func<Template, T?> instanceConstructor, bool allowMultipleInstantiations, DelegatedInstantiatorDisposer disposer) : base(templateConstructor, allowMultipleInstantiations)
            {
                this.instanceConstructor = instanceConstructor;
                this.disposer = disposer;
            }

            IDelegatedInstantiator<T, Template> IDelegatedInstantiator<T, Template>.AndThen(Action<T> action) => new ChildDelegatedInstantiator(this, action);

            IDelegatedInstantiator<T> IDelegatedInstantiator<T>.AndThen(Action<T> action) => new ChildDelegatedInstantiator(this, action);

            public override IGenericInstantiator<T, Template> AndThen(Action<T> action) => new ChildDelegatedInstantiator(this, action);

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

        private class ChildDelegatedInstantiator : IDelegatedInstantiator<T, Template>
        {
            private readonly IDelegatedInstantiator<T, Template> parent;
            private readonly Action<T> action;

            public ChildDelegatedInstantiator(IDelegatedInstantiator<T, Template> parent, Action<T> action)
            {
                this.parent = parent;
                this.action = action;
            }

            public bool IsExpired => parent.IsExpired;

            public IDelegatedInstantiator<T, Template> AndThen(Action<T> action) => new ChildDelegatedInstantiator(this, action);

            IDelegatedInstantiator<T> IDelegatedInstantiator<T>.AndThen(Action<T> action) => new ChildDelegatedInstantiator(this, action);

            IGenericInstantiator<T, Template> IGenericInstantiator<T, Template>.AndThen(Action<T> action) => new ChildDelegatedInstantiator(this, action);

            IInstantiatorBase<T> IInstantiatorBase<T>.AndThen(Action<T> action) => new ChildDelegatedInstantiator(this, action);

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

        private readonly List<WeakReference<GenericInstantiator>> instantiators = new();
        private readonly List<DelegatedInstantiatorDisposer> disposers = new();

        private bool disposed;

        public ITemplatedInstantiator<T, Template> Templated(Func<Template?> templateConstructor, bool allowMultipleInstantiations)
        {
            var instantiator = new TemplatedInstantiator(templateConstructor, allowMultipleInstantiations);
            AddInstantiator(instantiator);
            return instantiator;
        }

        public IDelegatedInstantiator<T, Template> Delegated(Func<Template?> templateConstructor, Func<Template, T?> instanceConstructor, Action<T> instanceDestructor, bool allowMultipleInstantiations)
        {
            var disposer = new DelegatedInstantiatorDisposer(this, instanceDestructor);
            var instantiator = new DelegatedInstantiator(templateConstructor, instanceConstructor, allowMultipleInstantiations, disposer);
            AddInstantiator(instantiator);
            return instantiator;
        }

        private void AddInstantiator(GenericInstantiator instantiator)
        {
            lock (instantiators)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                // Remove any instantiators that are
                // no longer referenced
                for (int i = instantiators.Count - 1; i >= 0; --i)
                {
                    if (!instantiators[i].TryGetTarget(out var _))
                    {
                        instantiators.RemoveAt(i);
                    }
                }

                instantiators.Add(new(instantiator));
            }
        }

        private void AddDisposer(DelegatedInstantiatorDisposer disposer)
        {
            lock (disposers)
            {
                if (!disposers.Contains(disposer))
                {
                    disposers.Add(disposer);
                }
            }
        }

        private void RemoveDisposer(DelegatedInstantiatorDisposer disposer)
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
                lock (instantiators)
                {
                    disposed = true;

                    // Dispose all instantiators that are
                    // still referenced somewhere so that
                    // they can't be used anymore
                    for (int i = instantiators.Count - 1; i >= 0; --i)
                    {
                        if (instantiators[i].TryGetTarget(out var instantiator))
                        {
                            instantiator.Dispose();
                        }
                        instantiators.RemoveAt(i);
                    }

                    // Dispose any "left over" delegated
                    // instantiator disposers to make sure
                    // that their instances are deconstructed
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
