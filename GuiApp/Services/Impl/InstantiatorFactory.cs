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

            private readonly List<Action<T>> actions = new();

            private readonly Func<Template?> templateConstructor;

            private readonly bool allowMultipleInstantiations;

            public GenericInstantiator(Func<Template?> templateConstructor, bool allowMultipleInstantiations)
            {
                this.templateConstructor = templateConstructor;
                this.allowMultipleInstantiations = allowMultipleInstantiations;
            }

            IInstantiatorBase<T> IInstantiatorBase<T>.AndThen(Action<T> action) => AndThen(action);

            public IGenericInstantiator<T, Template> AndThen(Action<T> action)
            {
                lock (this)
                {
                    CheckExpired();
                    actions.Add(action);
                }
                return this;
            }

            public abstract T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable);

            protected T InstantiateInternal(Func<Template, T?> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable)
            {
                lock (this)
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

                    foreach (var action in actions)
                    {
                        action.Invoke(instance!);
                    }

                    disposable = Disposable.Create(() => instanceDestructor.Invoke(instance!));

                    return instance!;
                }
            }

            private void CheckExpired()
            {
                if (IsExpired)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }

            private void Expire()
            {
                lock (this)
                {
                    IsExpired = true;
                }
            }

            public virtual void Dispose()
            {
                Expire();
            }
        }

        private class TemplatedInstantiator : GenericInstantiator, ITemplatedInstantiator<T, Template>
        {
            public TemplatedInstantiator(Func<Template?> templateConstructor, bool allowMultipleInstantiations) : base(templateConstructor, allowMultipleInstantiations)
            {
            }

            ITemplatedInstantiator<T, Template> ITemplatedInstantiator<T, Template>.AndThen(Action<T> action) => (ITemplatedInstantiator<T, Template>)AndThen(action);

            public T Instantiate(Func<Template, T> instanceConstructor)
            {
                return Instantiate(instanceConstructor, instance => { }, out var _ /* no disposal needed, caller is responsible for deconstruction */);
            }

            public override T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable)
            {
                return InstantiateInternal(instanceConstructor, instanceDestructor, out disposable);
            }
        }

        private class DelegatedInstantiator : GenericInstantiator, IDelegatedInstantiator<T, Template>
        {
            private readonly Func<Template, T?> instanceConstructor;
            private readonly Action<T> instanceDestructor;

            private readonly IDisposable disposable;

            private readonly List<T> instances = new();

            public DelegatedInstantiator(Func<Template?> templateConstructor, Func<Template, T?> instanceConstructor, Action<T> instanceDestructor, bool allowMultipleInstantiations, out CompositeDisposable disposable) : base(templateConstructor, allowMultipleInstantiations)
            {
                this.instanceConstructor = instanceConstructor;
                this.instanceDestructor = instanceDestructor;

                // Return a disposable to allow
                // the factory user to deconstruct
                // the created instances
                this.disposable = disposable = new CompositeDisposable();
                disposable.Add(Disposable.Create(() =>
                {
                    lock (this)
                    {
                        base.Dispose();

                        foreach (var instance in instances)
                        {
                            instanceDestructor.Invoke(instance);
                        }
                        instances.Clear();
                    }
                }));
            }

            IDelegatedInstantiator<T, Template> IDelegatedInstantiator<T, Template>.AndThen(Action<T> action) => (IDelegatedInstantiator<T, Template>)AndThen(action);

            IDelegatedInstantiator<T> IDelegatedInstantiator<T>.AndThen(Action<T> action) => (IDelegatedInstantiator<T>)AndThen(action);

            public T Instantiate(out IDisposable disposable)
            {
                lock (this)
                {
                    var newInstance = InstantiateInternal(instanceConstructor, instanceDestructor, out var instanceDisposable);

                    instances.Add(newInstance);

                    disposable = Disposable.Create(() =>
                    {
                        instances.Remove(newInstance);
                        instanceDisposable.Dispose();
                    });

                    return newInstance;
                }
            }

            public override T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable)
            {
                return Instantiate(out disposable);
            }

            public override void Dispose()
            {
                lock (this)
                {
                    base.Dispose();

                    disposable.Dispose();
                }
            }
        }

        private readonly List<WeakReference<GenericInstantiator>> instantiators = new();
        private readonly List<IDisposable> disposables = new();

        private bool disposed;

        public ITemplatedInstantiator<T, Template> Templated(Func<Template?> templateConstructor, bool allowMultipleInstantiations)
        {
            var instantiator = new TemplatedInstantiator(templateConstructor, allowMultipleInstantiations);

            AddInstantiator(instantiator);

            return instantiator;
        }

        public IDelegatedInstantiator<T, Template> Delegated(Func<Template?> templateConstructor, Func<Template, T?> instanceConstructor, Action<T> instanceDestructor, bool allowMultipleInstantiations)
        {
            var instantiator = new DelegatedInstantiator(templateConstructor, instanceConstructor, instanceDestructor, allowMultipleInstantiations, out var disposable);

            lock (disposables)
            {
                AddInstantiator(instantiator);

                // Make disposable remove itself
                // from the list when it's disposed
                disposable.Add(Disposable.Create(() =>
                {
                    lock (disposables)
                    {
                        disposables.Remove(disposable);
                    }
                }));

                disposables.Add(disposable);
            }

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

                RemoveUnreferencedInstantiators();

                instantiators.Add(new(instantiator));
            }
        }

        private void RemoveUnreferencedInstantiators()
        {
            lock (instantiators)
            {
                // Remove any instantiators that are
                // no longer referenced
                for (int i = instantiators.Count - 1; i >= 0; --i)
                {
                    if (!instantiators[i].TryGetTarget(out var _))
                    {
                        instantiators.RemoveAt(i);
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (disposables)
            {
                lock (instantiators)
                {
                    disposed = true;

                    // Dispose all instantiators that are
                    // still referenced somewhere so that
                    // they can't be used anymore
                    foreach (var wref in instantiators)
                    {
                        if (wref.TryGetTarget(out var instantiator))
                        {
                            instantiator.Dispose();
                        }
                    }
                    instantiators.Clear();

                    // Dispose any "left over" delegated
                    // instantiators to make sure that
                    // their instances are deconstructed
                    foreach (var disposable in disposables)
                    {
                        disposable.Dispose();
                    }
                    disposables.Clear();
                }
            }
        }
    }
}
