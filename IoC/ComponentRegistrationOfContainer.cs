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

namespace FitsRatingTool.IoC
{
    public abstract class ComponentRegistrationOfContainer<Base, CInstance, CParameter> : IComponentRegistration<Base>
        where Base : class
        where CInstance : class, Base
    {
        public virtual string Id { get; }

        public virtual string Name { get; }

        public virtual bool IsEnabled => true;

        protected virtual CParameter Parameter { get; }

        protected virtual bool IsSingleUse { get; set; } = false;

        [Import]
        protected Func<IFactoryRoot<CInstance, CParameter>> RootFactory { get; private set; } = null!;

        // NB: This container's parent is the component registry
        [Import]
        protected IContainer<CInstance, CParameter> Container { get; set; } = null!;

        protected ComponentRegistrationOfContainer(string id, string name, CParameter parameter)
        {
            Id = id;
            Name = name;
            Parameter = parameter;
        }

        protected ComponentRegistrationOfContainer(
            Func<IFactoryRoot<CInstance, CParameter>> rootFactory,
            IContainer<CInstance, CParameter> container,
            string id, string name, CParameter parameter)
            : this(id, name, parameter)
        {
            RootFactory = rootFactory;
            Container = container;
        }

        public IDelegatedFactory<Base> CreateFactory(CompositeDisposable disposable)
        {
            var root = RootFactory.Invoke();
            disposable.Add(root);
            return root.Delegated(Parameter, t =>
            {
                CheckInitialized();
                return Container.Instantiate(t);
            },
            i => Container.Destroy(i), IsSingleUse);
        }

        protected void CheckInitialized()
        {
            if (!Container.IsInitialized)
            {
                throw new InvalidOperationException("Component registration container is not initialized. Component registrations should only be instantiated by a component registry!");
            }
        }
    }
}
