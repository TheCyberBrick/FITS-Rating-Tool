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
using System.ComponentModel.Composition;
using System.Reactive;
using System.Reactive.Disposables;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public abstract class ComponentRegistrationOfSingleton<Base, Implementation> : IComponentRegistration<Base>
        where Base : class
        where Implementation : class, Base
    {
        public virtual string Id { get; }

        public virtual string Name { get; }

        protected virtual Implementation Instance { get; }

        [Import]
        protected Func<IFactoryRoot<Implementation, Unit>> RootFactory { get; private set; } = null!;

        public ComponentRegistrationOfSingleton(string id, string name, Implementation instance)
        {
            Id = id;
            Name = name;
            Instance = instance;
        }

        public IDelegatedFactory<Base> CreateFactory(CompositeDisposable disposable)
        {
            var root = RootFactory.Invoke();
            disposable.Add(root);
            return root.Delegated(Unit.Default, _ => Instance, _ => { }, false);
        }
    }
}
