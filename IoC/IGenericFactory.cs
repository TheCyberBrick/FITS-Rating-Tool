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
    public interface IGenericFactory<Instance, Parameter> : IFactoryBase<Instance>
    {
        /// <inheritdoc cref="IFactoryBase{T}.AndThen(Action{T})"/>
        new IGenericFactory<Instance, Parameter> AndThen(Action<Instance> action);

        /// <summary>
        /// Creates a new instance. The <paramref name="instanceConstructor"/> may or may not be used to construct the instance. However, if <paramref name="instanceConstructor"/> is used to
        /// construct the instance, then <paramref name="instanceDestructor"/> will be called exactly once when <paramref name="disposable"/> is disposed and/or the owning factory root is disposed.
        /// The caller is responsible for the deconstruction of the created instance by disposing <paramref name="disposable"/> when it is no longer needed.
        /// </summary>
        /// <param name="instanceConstructor">Function that constructs the instance from a parameter.</param>
        /// <param name="instanceDestructor">Function that deconstructs an instance created from the factory. Called exactly once for each instance created through <paramref name="instanceConstructor"/>.</param>
        /// <param name="disposable">Object to dispose when the instance is no longer needed.</param>
        /// <returns></returns>
        Instance Instantiate(Func<Parameter, Instance> instanceConstructor, Action<Instance> instanceDestructor, out IDisposable disposable);
    }
}
