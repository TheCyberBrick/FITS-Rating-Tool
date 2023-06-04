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

namespace FitsRatingTool.IoC
{
    public static class IContainerRootExtensions
    {
        public static IDisposable Instantiate<Instance, Parameter>(this IContainerRoot<Instance, Parameter> containerRoot, Parameter parameter, out IContainer<Instance, Parameter> container, out Instance instance, bool singleton = false)
            where Instance : class
        {
            var disposable = containerRoot.Initialize(out container, singleton);
            instance = container.Instantiate(parameter);
            return disposable;
        }

        public static IDisposable Instantiate<Instance, Parameter>(this IContainerRoot<Instance, Parameter> containerRoot, Func<IContainer<Instance, Parameter>, Instance> factory, out Instance instance, bool singleton = false)
            where Instance : class
        {
            var disposable = containerRoot.Initialize(out var container, singleton);
            instance = factory.Invoke(container);
            return disposable;
        }

        public static IDisposable Instantiate<Instance, Parameter>(this IContainerRoot<Instance, Parameter> containerRoot, Parameter parameter, out Instance instance, bool singleton = false)
            where Instance : class
        {
            return Instantiate(containerRoot, parameter, out var _, out instance, singleton);
        }
    }
}
