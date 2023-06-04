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
using System.Collections.Specialized;
using System.ComponentModel;

namespace FitsRatingTool.IoC.Impl
{
    internal class SingletonContainerWrapper<Instance, Parameter> : ISingletonContainer<Instance, Parameter>
        where Instance : class
    {
        private readonly IContainer<Instance, Parameter> container;
        private readonly IObservable<Instance?> singleton;
        public SingletonContainerWrapper(IContainer<Instance, Parameter> container, IObservable<Instance?> singleton)
        {
            this.container = container;
            this.singleton = singleton;
        }

        public Type InstanceType => container.InstanceType;

        public Type ParameterType => container.ParameterType;

        public bool IsInitialized => container.IsInitialized;

        public bool IsSingleton => container.IsSingleton;

        public int Count => container.Count;

        public event Action OnInitialized
        {
            add => container.OnInitialized += value;
            remove => container.OnInitialized -= value;
        }

        public event Action<Instance> OnInstantiated
        {
            add => container.OnInstantiated += value;
            remove => container.OnInstantiated -= value;
        }

        public event Action<Instance> OnDestroyed
        {
            add => container.OnDestroyed += value;
            remove => container.OnDestroyed -= value;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => container.PropertyChanged += value;
            remove => container.PropertyChanged -= value;
        }

        public event PropertyChangingEventHandler? PropertyChanging
        {
            add => container.PropertyChanging += value;
            remove => container.PropertyChanging -= value;
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => container.CollectionChanged += value;
            remove => container.CollectionChanged -= value;
        }

        public bool Destroy(Instance instance) => container.Destroy(instance);

        public void Destroy() => container.Destroy();

        public IEnumerator<Instance> GetEnumerator() => container.GetEnumerator();

        public Instance Instantiate(Parameter parameter) => container.Instantiate(parameter);

        public ISingletonContainer<Instance, Parameter> Singleton() => container.Singleton();

        IEnumerator IEnumerable.GetEnumerator() => container.GetEnumerator();

        public IDisposable Subscribe(IObserver<Instance?> observer) => singleton.Subscribe(observer);
    }
}
