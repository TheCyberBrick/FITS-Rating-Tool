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
    public static class IFactoryRootExtensions
    {
        public static IParameterizedFactory<Instance, Parameter> Parameterized<Instance, Parameter>(this IFactoryRoot<Instance, Parameter> factory, Parameter parameter, bool isSingleUse = true)
            where Instance : class
        {
            return factory.Parameterized(() => parameter, isSingleUse);
        }

        public static IDelegatedFactory<Instance, Parameter> Delegated<Instance, Parameter>(this IFactoryRoot<Instance, Parameter> factory, Parameter parameter, Func<Parameter, Instance?> instanceConstructor, Action<Instance> instanceDestructor, bool isSingleUse = true)
            where Instance : class
        {
            return factory.Delegated(() => parameter, instanceConstructor, instanceDestructor, isSingleUse);
        }

        public static IDelegatedFactory<Instance, Parameter> Delegated<Instance, Parameter>(this IFactoryRoot<Instance, Parameter> factory, Func<Parameter?> parameterConstructor, IContainer<Instance, Parameter> container, bool isSingleUse = true)
            where Instance : class
        {
            return factory.Delegated(parameterConstructor, container.Instantiate, instance => container.Destroy(instance), isSingleUse);
        }

        public static IDelegatedFactory<Instance, Parameter> Delegated<Instance, Parameter>(this IFactoryRoot<Instance, Parameter> factory, Parameter parameter, IContainer<Instance, Parameter> container, bool isSingleUse = true)
            where Instance : class
        {
            return factory.Delegated(() => parameter, container, isSingleUse);
        }
    }
}
