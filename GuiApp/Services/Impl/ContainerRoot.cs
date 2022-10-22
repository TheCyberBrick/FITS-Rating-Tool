﻿/*
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
using System.Reactive.Disposables;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class ContainerRoot<T, Template> : IContainerRoot<T, Template>
        where T : class
    {
        private readonly Func<IContainer<T, Template>> containerFactory;

        public ContainerRoot(Func<IContainer<T, Template>> containerFactory)
        {
            this.containerFactory = containerFactory;
        }

        public IDisposable Initialize(out IContainer<T, Template> container)
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

        public IDisposable Instantiate(Template template, out IContainer<T, Template> container)
        {
            var disposable = Initialize(out container);
            container.Instantiate(template);
            return disposable;
        }
    }

}
