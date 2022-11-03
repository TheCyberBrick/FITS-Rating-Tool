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
    public interface IParameterizedFactory<Instance, Parameter> : IGenericFactory<Instance, Parameter>
        where Instance : class
    {
        /// <inheritdoc cref="IFactoryBase{T}.AndThen(Action{T})"/>
        new IParameterizedFactory<Instance, Parameter> AndThen(Action<Instance> action);

        /// <summary>
        /// Creates a new instance through the specified constructor. The caller
        /// is responsible for the deconstruction of the created instance, if necessary.
        /// </summary>
        /// <param name="instanceConstructor">Function that constructs the instance from a parameter.</param>
        /// <returns></returns>
        Instance Instantiate(Func<Parameter, Instance> instanceConstructor);
    }
}
