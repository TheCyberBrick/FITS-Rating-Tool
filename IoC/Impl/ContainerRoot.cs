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
        private readonly Func<IContainer<Instance, Parameter>> containerFactory;

        public ContainerRoot(Func<IContainer<Instance, Parameter>> containerFactory)
        {
            this.containerFactory = containerFactory;
        }

        public IDisposable Initialize(out IContainer<Instance, Parameter> container)
        {
            container = containerFactory.Invoke();

            if (container is IContainerLifecycle lifecycle)
            {
                lifecycle.Initialize(null, null);
                return Disposable.Create(() => lifecycle.Destroy(true));
            }
            else
            {
                throw new InvalidOperationException($"Container does not implement {nameof(IContainerLifecycle)}");
            }
        }

        public IDisposable Instantiate(Parameter parameter, out IContainer<Instance, Parameter> container, out Instance instance)
        {
            var disposable = Initialize(out container);
            instance = container.Instantiate(parameter);
            return disposable;
        }
    }

}
