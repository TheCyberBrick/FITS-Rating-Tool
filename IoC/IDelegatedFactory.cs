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
    public interface IDelegatedFactory<out Instance> : IFactoryBase<Instance>
        where Instance : class
    {
        /// <inheritdoc cref="IFactoryBase{T}.AndThen(Action{T})"/>
        new IDelegatedFactory<Instance> AndThen(Action<Instance> action);

        /// <summary>
        /// Creates a new instance. The caller should deconstruct the created instance by disposing <paramref name="disposable"/>
        /// when it is no longer needed, otherwise the instance will stay around until the owning factory root is disposed.
        /// </summary>
        /// <param name="disposable">Object to dispose when the instance is no longer needed.</param>
        /// <returns></returns>
        Instance Instantiate(out IDisposable disposable);
    }

    public interface IDelegatedFactory<Instance, Parameter> : IGenericFactory<Instance, Parameter>, IDelegatedFactory<Instance>
        where Instance : class
    {
        /// <inheritdoc cref="IFactoryBase{T}.AndThen(Action{T})"/>
        new IDelegatedFactory<Instance, Parameter> AndThen(Action<Instance> action);
    }
}
