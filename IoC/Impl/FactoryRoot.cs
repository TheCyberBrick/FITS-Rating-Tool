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

using System.ComponentModel.Composition;
using System.Reactive.Disposables;

namespace FitsRatingTool.IoC.Impl
{
    [Export(typeof(IFactoryRoot<,>)), PartCreationPolicy(CreationPolicy.NonShared)]
    public class FactoryRoot<Instance, Parameter> : IFactoryRoot<Instance, Parameter>
        where Instance : class
    {
        private abstract class GenericFactory : IGenericFactory<Instance, Parameter>, IDisposable
        {
            public bool IsSingleUse { get; private set; }

            public Type InstanceType => typeof(Instance);

            public Type ParameterType => typeof(Parameter);

            private bool expired;

            private readonly Func<Parameter?> parameterConstructor;

            public GenericFactory(Func<Parameter?> parameterConstructor, bool isSingleUse)
            {
                this.parameterConstructor = parameterConstructor;
                IsSingleUse = isSingleUse;
            }

            IFactoryBase<Instance> IFactoryBase<Instance>.AndThen(Action<Instance> action) => AndThen(action);

            public abstract IGenericFactory<Instance, Parameter> AndThen(Action<Instance> action);

            public abstract Instance Instantiate(Func<Parameter, Instance> instanceConstructor, Action<Instance> instanceDestructor, out IDisposable disposable);

            protected Instance InstantiateInternal(Func<Parameter, Instance?> instanceConstructor)
            {
                CheckExpired();

                if (IsSingleUse)
                {
                    Expire();
                }

                var parameter = parameterConstructor.Invoke();

                if (parameter == null)
                {
                    Dispose();
                    CheckExpired();
                }

                var instance = instanceConstructor.Invoke(parameter!);

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

        private class ParameterizedFactory : GenericFactory, IParameterizedFactory<Instance, Parameter>
        {
            public ParameterizedFactory(Func<Parameter?> parameterConstructor, bool isSingleUse) : base(parameterConstructor, isSingleUse)
            {
            }

            IParameterizedFactory<Instance, Parameter> IParameterizedFactory<Instance, Parameter>.AndThen(Action<Instance> action) => new ChildParameterizedFactory(this, action);

            public override IGenericFactory<Instance, Parameter> AndThen(Action<Instance> action) => new ChildParameterizedFactory(this, action);

            public Instance Instantiate(Func<Parameter, Instance> instanceConstructor)
            {
                lock (this)
                {
                    return InstantiateInternal(instanceConstructor);
                }
            }

            public override Instance Instantiate(Func<Parameter, Instance> instanceConstructor, Action<Instance> instanceDestructor, out IDisposable disposable)
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

        private class ChildParameterizedFactory : IParameterizedFactory<Instance, Parameter>
        {
            public bool IsSingleUse => parent.IsSingleUse;

            public Type InstanceType => parent.InstanceType;

            public Type ParameterType => parent.ParameterType;

            private readonly IParameterizedFactory<Instance, Parameter> parent;
            private readonly Action<Instance> action;

            public ChildParameterizedFactory(IParameterizedFactory<Instance, Parameter> parent, Action<Instance> action)
            {
                this.parent = parent;
                this.action = action;
            }

            public IParameterizedFactory<Instance, Parameter> AndThen(Action<Instance> action) => new ChildParameterizedFactory(this, action);

            IGenericFactory<Instance, Parameter> IGenericFactory<Instance, Parameter>.AndThen(Action<Instance> action) => new ChildParameterizedFactory(this, action);

            IFactoryBase<Instance> IFactoryBase<Instance>.AndThen(Action<Instance> action) => new ChildParameterizedFactory(this, action);

            public Instance Instantiate(Func<Parameter, Instance> instanceConstructor)
            {
                var newInstance = parent.Instantiate(instanceConstructor);
                action.Invoke(newInstance);
                return newInstance;
            }

            public Instance Instantiate(Func<Parameter, Instance> instanceConstructor, Action<Instance> instanceDestructor, out IDisposable disposable)
            {
                var newInstance = parent.Instantiate(instanceConstructor, instanceDestructor, out disposable);
                action.Invoke(newInstance);
                return newInstance;
            }
        }

        private class DelegatedFactoryDisposer : IDisposable
        {
            private readonly List<Instance> instances = new();

            private readonly FactoryRoot<Instance, Parameter> factory;
            private readonly Action<Instance> instanceDestructor;

            public DelegatedFactoryDisposer(FactoryRoot<Instance, Parameter> factory, Action<Instance> instanceDestructor)
            {
                this.instanceDestructor = instanceDestructor;
                this.factory = factory;
            }

            public void AddInstance(Instance instance)
            {
                lock (this)
                {
                    instances.Add(instance);

                    factory.AddDisposer(this);
                }
            }

            public void RemoveInstance(Instance instance)
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

        private class DelegatedFactory : GenericFactory, IDelegatedFactory<Instance, Parameter>
        {
            private readonly Func<Parameter, Instance?> instanceConstructor;
            private readonly DelegatedFactoryDisposer disposer;

            public DelegatedFactory(Func<Parameter?> parameterConstructor, Func<Parameter, Instance?> instanceConstructor, bool isSingleUse, DelegatedFactoryDisposer disposer) : base(parameterConstructor, isSingleUse)
            {
                this.instanceConstructor = instanceConstructor;
                this.disposer = disposer;
            }

            IDelegatedFactory<Instance, Parameter> IDelegatedFactory<Instance, Parameter>.AndThen(Action<Instance> action) => new ChildDelegatedFactory(this, action);

            IDelegatedFactory<Instance> IDelegatedFactory<Instance>.AndThen(Action<Instance> action) => new ChildDelegatedFactory(this, action);

            public override IGenericFactory<Instance, Parameter> AndThen(Action<Instance> action) => new ChildDelegatedFactory(this, action);

            public Instance Instantiate(out IDisposable disposable)
            {
                lock (disposer)
                {
                    var newInstance = InstantiateInternal(instanceConstructor);

                    disposer.AddInstance(newInstance);

                    disposable = Disposable.Create(() => disposer.RemoveInstance(newInstance));

                    return newInstance;
                }
            }

            public override Instance Instantiate(Func<Parameter, Instance> instanceConstructor, Action<Instance> instanceDestructor, out IDisposable disposable)
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

        private class ChildDelegatedFactory : IDelegatedFactory<Instance, Parameter>
        {
            public bool IsSingleUse => parent.IsSingleUse;

            public Type InstanceType => parent.InstanceType;

            public Type ParameterType => parent.ParameterType;

            private readonly IDelegatedFactory<Instance, Parameter> parent;
            private readonly Action<Instance> action;

            public ChildDelegatedFactory(IDelegatedFactory<Instance, Parameter> parent, Action<Instance> action)
            {
                this.parent = parent;
                this.action = action;
            }

            public IDelegatedFactory<Instance, Parameter> AndThen(Action<Instance> action) => new ChildDelegatedFactory(this, action);

            IDelegatedFactory<Instance> IDelegatedFactory<Instance>.AndThen(Action<Instance> action) => new ChildDelegatedFactory(this, action);

            IGenericFactory<Instance, Parameter> IGenericFactory<Instance, Parameter>.AndThen(Action<Instance> action) => new ChildDelegatedFactory(this, action);

            IFactoryBase<Instance> IFactoryBase<Instance>.AndThen(Action<Instance> action) => new ChildDelegatedFactory(this, action);

            public Instance Instantiate(Func<Parameter, Instance> instanceConstructor, Action<Instance> instanceDestructor, out IDisposable disposable)
            {
                var newInstance = parent.Instantiate(instanceConstructor, instanceDestructor, out disposable);
                action.Invoke(newInstance);
                return newInstance;
            }

            public Instance Instantiate(out IDisposable disposable)
            {
                var newInstance = parent.Instantiate(out disposable);
                action.Invoke(newInstance);
                return newInstance;
            }
        }

        private readonly List<WeakReference<GenericFactory>> factories = new();
        private readonly List<DelegatedFactoryDisposer> disposers = new();

        private bool disposed;

        public IParameterizedFactory<Instance, Parameter> Parameterized(Func<Parameter?> parameterConstructor, bool isSingleUse)
        {
            var factory = new ParameterizedFactory(parameterConstructor, isSingleUse);
            AddFactory(factory);
            return factory;
        }

        public IDelegatedFactory<Instance, Parameter> Delegated(Func<Parameter?> parameterConstructor, Func<Parameter, Instance?> instanceConstructor, Action<Instance> instanceDestructor, bool isSingleUse)
        {
            var disposer = new DelegatedFactoryDisposer(this, instanceDestructor);
            var factory = new DelegatedFactory(parameterConstructor, instanceConstructor, isSingleUse, disposer);
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
