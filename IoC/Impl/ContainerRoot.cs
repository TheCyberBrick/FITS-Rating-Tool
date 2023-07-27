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
    [Export(typeof(IContainerRoot<,>)), PartCreationPolicy(CreationPolicy.NonShared)]
    public class ContainerRoot<Instance, Parameter> : IContainerRoot<Instance, Parameter>
        where Instance : class
    {
        private readonly IContainerResolver containerResolver;
        private readonly Func<IContainerResolver.IScope, IContainer<Instance, Parameter>> containerFactory;

        private readonly IContainerResolver.IScope? parentScope;

        public ContainerRoot(IContainerResolver containerResolver, Func<IContainerResolver.IScope, IContainer<Instance, Parameter>> containerFactory)
        {
            this.containerResolver = containerResolver;
            this.containerFactory = containerFactory;
            this.parentScope = null;
        }

        public ContainerRoot(IContainerResolver containerResolver, Func<IContainerResolver.IScope, IContainer<Instance, Parameter>> containerFactory, IContainerResolver.IScope parentScope)
        {
            this.containerResolver = containerResolver;
            this.containerFactory = containerFactory;
            this.parentScope = parentScope;
        }

        public IDisposable Initialize(out IContainer<Instance, Parameter> container, bool singleton = false, bool inheritParentScopes = true)
        {
            var rootScope = containerResolver.OpenScopes(inheritParentScopes ? parentScope : null, true);

            container = containerFactory.Invoke(rootScope);

            if (singleton)
            {
                container.Singleton();
            }

            if (container is IContainerLifecycle lifecycle)
            {
                if (!lifecycle.IsInitialized)
                {
                    lifecycle.Initialize(rootScope, null, null);
                }

                return Disposable.Create(() =>
                {
                    try
                    {
                        lifecycle.Destroy(true);
                    }
                    finally
                    {
                        rootScope.Dispose();
                    }
                });
            }
            else
            {
                throw new InvalidOperationException($"Container does not implement {nameof(IContainerLifecycle)}");
            }
        }
    }

}
