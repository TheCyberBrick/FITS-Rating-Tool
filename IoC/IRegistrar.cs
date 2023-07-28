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

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FitsRatingTool.IoC
{
    public interface IRegistrar<Instance, Parameter>
        where Instance : class
    {
        object ClassScopeName { get; }

        IRegistrar<Instance, Parameter> WithConstructor(ConstructorInfo constructor);

        IRegistrar<Instance, Parameter> WithScopes(params object[] scopeNames);

        IRegistrar<Instance, Parameter> WithInitializer(Func<Parameter, Parameter> initializer);

        [DoesNotReturn]
        void RegisterAndReturn<Implementation>()
            where Implementation : class, Instance;
    }
}
